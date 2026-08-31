using System.Net;
using FunNTalk.Domain.DTOs;
using FunNTalk.Infrastructure.Configuration;
using FunNTalk.Infrastructure.UseCases;
using FunNTalk.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FunNTalk.Tests.Infrastructure.UseCases;

[TestClass]
public class GetIceServersUseCaseTests
{
    private const string Stun = "stun:stun.l.google.com:19302";
    private const string Turn = "turn:relay.example.com:80";

    private const string MintPayload =
        """
        {
          "username": "minted-user",
          "password": "minted-secret",
          "expiryInSeconds": 7200,
          "label": "funntalk",
          "apiKey": "minted-api-key"
        }
        """;

    private const string ProviderPayload =
        """
        [
          { "urls": "stun:stun.l.google.com:19302" },
          { "urls": "turn:relay.example.com:80", "username": "minted-user", "credential": "minted-secret" },
          { "urls": "turns:relay.example.com:443", "username": "minted-user", "credential": "minted-secret" }
        ]
        """;

    [TestMethod]
    public async Task ExecuteAsync_WithoutAMeteredProvider_ReturnsTheConfiguredStunOnly()
    {
        var handler = FakeHttpMessageHandler.Throwing(
            new InvalidOperationException("No provider is configured, so no request may be made."));
        var useCase = CreateUseCase(handler, new IceServerOptions { StunUrls = [Stun] });

        var servers = await useCase.ExecuteAsync();

        AssertStunOnly(servers);
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    [DataRow(null, "an-api-key")]
    [DataRow("funntalk", null)]
    [DataRow("", "an-api-key")]
    [DataRow("funntalk", "   ")]
    public async Task ExecuteAsync_WithAnIncompleteMeteredConfiguration_MakesNoProviderCall(
        string? subdomain, string? apiKey)
    {
        var handler = FakeHttpMessageHandler.Throwing(
            new InvalidOperationException("The provider is not fully configured, so no request may be made."));
        var options = new IceServerOptions
        {
            StunUrls = [Stun],
            Metered = new IceServerOptions.MeteredOptions { Subdomain = subdomain, ApiKey = apiKey },
        };

        var servers = await CreateUseCase(handler, options).ExecuteAsync();

        AssertStunOnly(servers);
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WithAConfiguredProvider_ReturnsTheStunPlusTheProvidersTurnEntries()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.RespondingWithJson(ProviderPayload), ConfiguredOptions());

        var servers = await useCase.ExecuteAsync();

        Assert.HasCount(3, servers);
        Assert.AreEqual(new IceServerDto(Stun), servers[0]);

        var turn = Assert.ContainsSingle(server => server.Urls == Turn, servers);
        Assert.AreEqual("minted-user", turn.Username);
        Assert.AreEqual("minted-secret", turn.Credential);
        Assert.Contains(server => server.Urls == "turns:relay.example.com:443", servers);
    }

    [TestMethod]
    public async Task ExecuteAsync_DoesNotReturnAProviderEntryThatDuplicatesAConfiguredStunUrl()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.RespondingWithJson(ProviderPayload), ConfiguredOptions());

        var servers = await useCase.ExecuteAsync();

        Assert.ContainsSingle(server => server.Urls == Stun, servers);
        Assert.AreEqual(servers.Count, servers.Select(server => server.Urls).Distinct().Count());
    }

    [TestMethod]
    public async Task ExecuteAsync_BuildsTheRequestUrlFromTheConfiguredSubdomainAndApiKey()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(ProviderPayload);

        await CreateUseCase(handler, ConfiguredOptions()).ExecuteAsync();

        var requestUri = Assert.ContainsSingle(handler.RequestedUris);
        Assert.IsNotNull(requestUri);
        Assert.Contains("funntalk", requestUri!.ToString(), StringComparison.Ordinal);
        Assert.Contains("apiKey=an-api-key", requestUri.ToString(), StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenTheProviderFailsWithAServerError_FallsBackToStunOnly()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.Responding(HttpStatusCode.InternalServerError, "boom"), ConfiguredOptions());

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenTheProviderCallThrows_FallsBackToStunOnly()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.Throwing(new HttpRequestException("connection refused")), ConfiguredOptions());

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenTheProviderTimesOut_FallsBackToStunOnly()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.Throwing(new TaskCanceledException("timed out")), ConfiguredOptions());

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenTheProviderReturnsMalformedJson_FallsBackToStunOnly()
    {
        var useCase = CreateUseCase(
            FakeHttpMessageHandler.RespondingWithJson("this is not json"), ConfiguredOptions());

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenTheProviderReturnsJsonNull_FallsBackToStunOnly()
    {
        var useCase = CreateUseCase(FakeHttpMessageHandler.RespondingWithJson("null"), ConfiguredOptions());

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_SkipsConfiguredStunUrlsThatAreBlank()
    {
        var handler = FakeHttpMessageHandler.Throwing(new InvalidOperationException("No request may be made."));
        var useCase = CreateUseCase(handler, new IceServerOptions { StunUrls = [Stun, "   ", ""] });

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_DeduplicatesRepeatedStunUrls()
    {
        // The configuration binder appends bound array items onto the property default rather
        // than replacing them, so an appsettings entry matching the default arrives twice.
        var handler = FakeHttpMessageHandler.Throwing(new InvalidOperationException("No request may be made."));
        var useCase = CreateUseCase(handler, new IceServerOptions { StunUrls = [Stun, Stun] });

        AssertStunOnly(await useCase.ExecuteAsync());
    }

    [TestMethod]
    public async Task ExecuteAsync_WithASecretKey_MintsACredentialThenRedeemsItForTheIceServers()
    {
        var handler = MintingHandler();

        var servers = await CreateUseCase(handler, MintingOptions()).ExecuteAsync();

        Assert.AreEqual(2, handler.CallCount);
        Assert.Contains(
            "/api/v1/turn/credential?secretKey=a-secret-key",
            handler.RequestedUris[0]!.ToString(),
            StringComparison.Ordinal);
        Assert.Contains(
            "/api/v1/turn/credentials?apiKey=minted-api-key",
            handler.RequestedUris[1]!.ToString(),
            StringComparison.Ordinal);
        Assert.IsTrue(servers.Any(server => server.Urls == Turn && server.Credential is not null));
    }

    [TestMethod]
    public async Task ExecuteAsync_WithASecretKey_AsksForTheConfiguredExpiry()
    {
        var handler = MintingHandler();
        var options = MintingOptions();
        options.Metered.CredentialTtlSeconds = 900;

        await CreateUseCase(handler, options).ExecuteAsync();

        Assert.Contains("900", handler.RequestBodies[0], StringComparison.Ordinal);
        Assert.Contains("expiryInSeconds", handler.RequestBodies[0], StringComparison.Ordinal);
    }

    /// <summary>
    /// The standing API key must play no part once minting is available: redeeming it instead of
    /// the minted one would hand the browser a credential that never expires, which is the whole
    /// defect this path exists to close.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithASecretKey_RedeemsTheMintedKeyRatherThanTheStandingApiKey()
    {
        var handler = MintingHandler();
        var options = MintingOptions();
        options.Metered.ApiKey = "the-standing-api-key";

        await CreateUseCase(handler, options).ExecuteAsync();

        var redeemUri = handler.RequestedUris[1]!.ToString();
        Assert.Contains("apiKey=minted-api-key", redeemUri, StringComparison.Ordinal);
        Assert.DoesNotContain("the-standing-api-key", redeemUri, StringComparison.Ordinal);
    }

    /// <summary>
    /// Regression guard for deployments that only ever had an API key: they must keep working,
    /// degraded but functional, rather than losing TURN the moment this path ships.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithoutASecretKey_StillUsesTheStandingCredentialPath()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson(ProviderPayload);

        var servers = await CreateUseCase(handler, ConfiguredOptions()).ExecuteAsync();

        var requestUri = Assert.ContainsSingle(handler.RequestedUris);
        Assert.Contains("apiKey=an-api-key", requestUri!.ToString(), StringComparison.Ordinal);
        Assert.IsTrue(servers.Any(server => server.Urls == Turn));
    }

    [TestMethod]
    public async Task ExecuteAsync_WithASecretKeyButNoSubdomain_MakesNoProviderCall()
    {
        var handler = FakeHttpMessageHandler.Throwing(
            new InvalidOperationException("Without a subdomain there is no host to call."));
        var options = new IceServerOptions
        {
            StunUrls = [Stun],
            Metered = new IceServerOptions.MeteredOptions { SecretKey = "a-secret-key" },
        };

        AssertStunOnly(await CreateUseCase(handler, options).ExecuteAsync());
        Assert.AreEqual(0, handler.CallCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenMintingFailsWithAServerError_FallsBackToStunOnly()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.Unauthorized, "authorization failed");

        AssertStunOnly(await CreateUseCase(handler, MintingOptions()).ExecuteAsync());
        Assert.AreEqual(1, handler.CallCount);
    }

    /// <summary>
    /// A 200 that carries no apiKey is not a usable credential. Redeeming the empty value would
    /// send a malformed request instead of failing where the fault actually is.
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WhenTheMintResponseOmitsTheApiKey_FallsBackToStunOnly()
    {
        var handler = FakeHttpMessageHandler.RespondingWithJson("""{ "username": "u", "password": "p" }""");

        AssertStunOnly(await CreateUseCase(handler, MintingOptions()).ExecuteAsync());
        Assert.AreEqual(1, handler.CallCount);
    }

    private static FakeHttpMessageHandler MintingHandler() =>
        FakeHttpMessageHandler.RespondingPerRequest(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.Method == HttpMethod.Post ? MintPayload : ProviderPayload),
        });

    private static IceServerOptions MintingOptions() => new()
    {
        StunUrls = [Stun],
        Metered = new IceServerOptions.MeteredOptions
        {
            Subdomain = "funntalk",
            SecretKey = "a-secret-key",
        },
    };

    private static void AssertStunOnly(IReadOnlyList<IceServerDto> servers)
    {
        var only = Assert.ContainsSingle(servers);
        Assert.AreEqual(Stun, only.Urls);
        Assert.IsNull(only.Username);
        Assert.IsNull(only.Credential);
    }

    private static IceServerOptions ConfiguredOptions() => new()
    {
        StunUrls = [Stun],
        Metered = new IceServerOptions.MeteredOptions { Subdomain = "funntalk", ApiKey = "an-api-key" },
    };

    // The use case is internal, and a dynamic proxy cannot close a generic over an inaccessible
    // type, so the logger is a real no-op instead of a substitute.
    private static GetIceServersUseCase CreateUseCase(FakeHttpMessageHandler handler, IceServerOptions options) =>
        new(new HttpClient(handler),
            new OptionsWrapper<IceServerOptions>(options),
            NullLogger<GetIceServersUseCase>.Instance);
}
