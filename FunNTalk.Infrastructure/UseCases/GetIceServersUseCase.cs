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
/// TURN credentials cannot be kept secret in a browser: whatever the client uses to connect
/// is readable from the network tab. The defence is that the provider mints them per request
/// and they expire on their own, so a stolen credential dies without intervention. The API key
/// that mints them never leaves the server.
/// </summary>
internal sealed class GetIceServersUseCase(
    HttpClient httpClient,
    IOptions<IceServerOptions> options,
    ILogger<GetIceServersUseCase> logger) : IGetIceServersUseCase
{
    private static readonly JsonSerializerOptions ProviderJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient = httpClient;
    private readonly IceServerOptions _options = options.Value;
    private readonly ILogger<GetIceServersUseCase> _logger = logger;

    public async Task<IReadOnlyList<IceServerDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var stunServers = BuildStunServers();

        if (!_options.Metered.IsConfigured)
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

    private async Task<IReadOnlyList<IceServerDto>> FetchMeteredServersAsync(CancellationToken cancellationToken)
    {
        var requestUri =
            $"https://{_options.Metered.Subdomain}.metered.live/api/v1/turn/credentials?apiKey={Uri.EscapeDataString(_options.Metered.ApiKey!)}";

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
