namespace FunNTalk.Infrastructure.Configuration;

public sealed class IceServerOptions
{
    public const string SectionName = "IceServers";

    /// <summary>
    /// STUN servers returned on every response. They are also the whole response when no
    /// TURN provider is configured or the provider call fails.
    /// </summary>
    public string[] StunUrls { get; set; } = ["stun:stun.l.google.com:19302"];

    public MeteredOptions Metered { get; set; } = new();

    public sealed class MeteredOptions
    {
        /// <summary>Metered subdomain, e.g. "funntalk" for funntalk.metered.live.</summary>
        public string? Subdomain { get; set; }

        /// <summary>
        /// Secret Metered API key. It stays on the server and never reaches the browser,
        /// so it belongs in user secrets or an environment variable, not in appsettings.
        /// </summary>
        public string? ApiKey { get; set; }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Subdomain) && !string.IsNullOrWhiteSpace(ApiKey);
    }
}
