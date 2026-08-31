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
        /// Metered API key for the account's standing credentials. It stays on the server and
        /// never reaches the browser, so it belongs in user secrets or an environment variable,
        /// not in appsettings.
        ///
        /// The credentials it returns do NOT expire. Prefer <see cref="SecretKey"/>.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Metered secret key. This is the only credential that can mint *expiring* TURN
        /// credentials, and it is a different value from <see cref="ApiKey"/> — the create
        /// endpoint rejects an API key outright.
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>
        /// How long a minted credential stays valid. It has to outlast the longest call a user
        /// might hold, because the credential is issued once at join time and never renewed
        /// mid-session. Two hours by default.
        /// </summary>
        public int CredentialTtlSeconds { get; set; } = 7200;

        /// <summary>
        /// True when the standing-credential path is usable. Kept as a fallback so an existing
        /// deployment that only has an API key keeps working.
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Subdomain) && !string.IsNullOrWhiteSpace(ApiKey);

        /// <summary>
        /// True when credentials can be minted with an expiry. Minting needs only the subdomain
        /// and the secret key: <see cref="ApiKey"/> plays no part, because every minted
        /// credential carries an API key of its own.
        /// </summary>
        public bool CanMintExpiringCredentials =>
            !string.IsNullOrWhiteSpace(Subdomain) && !string.IsNullOrWhiteSpace(SecretKey);
    }
}
