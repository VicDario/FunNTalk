namespace FunNTalk.Infrastructure.Configuration;

/// <summary>
/// Room lifecycle knobs. Every property is a scalar by design: the configuration binder appends
/// bound array items onto a property's existing default instead of replacing them (the verified
/// cause of a duplicate-STUN bug in <c>IceServerOptions</c>), and a scalar cannot fall into that
/// trap. Code length is deliberately NOT here — it is <c>RoomCodeExtensions.Length</c>, a Domain
/// constant the generator and the validator both read, so they cannot disagree at runtime.
/// </summary>
public sealed class RoomOptions
{
    public const string SectionName = "Rooms";

    /// <summary>How long a room may sit with zero participants before it is reclaimed.</summary>
    public int VacancyTtlMinutes { get; set; } = 10;

    /// <summary>How often the reaper checks for vacant rooms past their TTL.</summary>
    public int SweepIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Bound on generate-and-reserve retries before creation fails loudly. This is a saturation
    /// alarm, not a tuning knob: with a 32^6 code space, exhausting this bound only happens under
    /// astronomically implausible occupancy.
    /// </summary>
    public int MaxMintAttempts { get; set; } = 5;

    /// <summary>
    /// A misconfigured TTL is a security/availability property, not something to discover at
    /// runtime — a zero TTL would reclaim a room before its creator can even read the code aloud.
    /// </summary>
    public static bool Validate(RoomOptions options) =>
        options.VacancyTtlMinutes > 0 && options.SweepIntervalSeconds > 0 && options.MaxMintAttempts >= 1;
}
