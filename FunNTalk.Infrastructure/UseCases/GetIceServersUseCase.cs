using System.Net.Http.Json;
using System.Text.Json;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.UseCases;
using FunNTalk.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunNTalk.Infrastructure.UseCases;

/// <summary>
/// Issues the ICE configuration the browser needs to negotiate a peer connection.
///
/// TURN credentials cannot be kept secret in a browser: whatever the client uses to connect is
/// readable from the network tab. The only real defence is that a stolen credential expires on
/// its own, so it dies without anyone having to notice.
///
/// That defence has to be bought deliberately. Metered's <c>GET /api/v1/turn/credentials</c>
/// hands back the account's *standing* credentials — verified against production, two calls
/// return byte-identical values, and they stay valid until someone rotates them by hand. Only
/// <c>POST /api/v1/turn/credential</c>, authenticated with the secret key, mints one that
/// expires. So when a secret key is configured this mints per request and redeems the minted
/// key; otherwise it falls back to the standing credential and says so in the log.
///
/// Neither the secret key nor the account API key ever leaves the server.
/// </summary>
internal sealed class GetIceServersUseCase(
    HttpClient httpClient,
    IOptions<IceServerOptions> options,
    ILogger<GetIceServersUseCase> logger) : IGetIceServersUseCase
{
    // Web defaults give camelCase on the way out, which the create endpoint requires
    // ("expiryInSeconds"), and case-insensitive matching on the way back in.
    private static readonly JsonSerializerOptions ProviderJsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record MintRequest(int ExpiryInSeconds, string Label);

    private sealed record MintResponse(string? Username, string? Password, string? ApiKey);

    private readonly HttpClient _httpClient = httpClient;
    private readonly IceServerOptions _options = options.Value;
    private readonly ILogger<GetIceServersUseCase> _logger = logger;

    public async Task<IReadOnlyList<IceServerDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stunServers = BuildStunServers();

        if (!_options.Metered.CanMintExpiringCredentials && !_options.Metered.IsConfigured)
        {
            _logger.LogWarning(
                "No TURN provider configured. Returning STUN only, so peers behind symmetric NAT will not connect.");
            return stunServers;
        }

        try
        {
            var providerServers = await FetchMeteredServersAsync(cancellationToken);
            return Merge(stunServers, providerServers);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(
                exception,
                "Could not mint TURN credentials. Falling back to STUN only, so peers behind symmetric NAT will not connect.");
            return stunServers;
        }
    }

    /// <summary>
    /// Deduplicated: the configuration binder appends bound array items onto the property's
    /// default instead of replacing them, so a StunUrls entry that repeats the default arrives
    /// twice and the browser would be handed the same server twice.
    /// </summary>
    private List<IceServerDto> BuildStunServers() =>
        [.. _options.StunUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(url => new IceServerDto(url))];

    /// <summary>
    /// Minting costs a second round trip, and both share the client's 5s budget. That is the
    /// price of a credential that expires; a timeout still degrades to STUN rather than hanging
    /// the caller.
    /// </summary>
    private async Task<IReadOnlyList<IceServerDto>> FetchMeteredServersAsync(CancellationToken cancellationToken)
    {
        string apiKey;

        if (_options.Metered.CanMintExpiringCredentials)
        {
            apiKey = await MintExpiringApiKeyAsync(cancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "No Metered secret key configured, so the account's standing TURN credentials are being served. "
                + "They do not expire, and anyone who reads them from the network tab keeps working access "
                + "until they are rotated by hand.");
            apiKey = _options.Metered.ApiKey!;
        }

        return await RedeemApiKeyAsync(apiKey, cancellationToken);
    }

    /// <summary>
    /// Creates a credential that dies on its own. The secret key is the only thing the provider
    /// accepts here — an account API key is rejected outright — and the response carries an API
    /// key of its own, scoped to the credential just minted.
    /// </summary>
    private async Task<string> MintExpiringApiKeyAsync(CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://{_options.Metered.Subdomain}.metered.live/api/v1/turn/credential?secretKey={Uri.EscapeDataString(_options.Metered.SecretKey!)}";

        using var response = await _httpClient.PostAsJsonAsync(
            requestUri,
            new MintRequest(_options.Metered.CredentialTtlSeconds, _options.Metered.Subdomain!),
            ProviderJsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        // Deserialized off the stream, the same way the redeem call does it, so both provider
        // responses fail in exactly one way. ReadFromJsonAsync also negotiates on Content-Type,
        // and a NotSupportedException from that check is not in the catch list below, so it would
        // escape instead of degrading to STUN.
        await using var payload = await response.Content.ReadAsStreamAsync(cancellationToken);
        var minted = await JsonSerializer.DeserializeAsync<MintResponse>(
            payload, ProviderJsonOptions, cancellationToken);

        // A 200 carrying no apiKey is not a credential. Redeeming the empty value would send a
        // malformed request and surface the fault one step away from where it actually happened.
        if (string.IsNullOrWhiteSpace(minted?.ApiKey))
        {
            throw new JsonException(
                "The provider accepted the mint request but returned no apiKey, so there is nothing to redeem.");
        }

        return minted.ApiKey;
    }

    private async Task<IReadOnlyList<IceServerDto>> RedeemApiKeyAsync(
        string apiKey, CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://{_options.Metered.Subdomain}.metered.live/api/v1/turn/credentials?apiKey={Uri.EscapeDataString(apiKey)}";

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var payload = await response.Content.ReadAsStreamAsync(cancellationToken);
        var servers = await JsonSerializer.DeserializeAsync<List<IceServerDto>>(
            payload, ProviderJsonOptions, cancellationToken);

        return servers ?? [];
    }

    /// <summary>
    /// The provider already answers in RTCIceServer shape and usually includes its own STUN
    /// entries, so entries are merged on <c>urls</c> to avoid handing the browser duplicates.
    /// </summary>
    private static List<IceServerDto> Merge(List<IceServerDto> stunServers, IReadOnlyList<IceServerDto> providerServers)
    {
        var merged = new List<IceServerDto>(stunServers);
        var knownUrls = new HashSet<string>(merged.Select(server => server.Urls), StringComparer.OrdinalIgnoreCase);

        foreach (var server in providerServers)
        {
            if (string.IsNullOrWhiteSpace(server.Urls)) continue;
            if (!knownUrls.Add(server.Urls)) continue;
            merged.Add(server);
        }

        return merged;
    }
}
