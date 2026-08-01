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
