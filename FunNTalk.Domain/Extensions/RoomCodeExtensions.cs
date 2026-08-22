namespace FunNTalk.Domain.Extensions;

/// <summary>
/// Shape rules for room codes, shared by the generator (<c>CreateRoomUseCase</c>) and every
/// entry point that accepts a code from a caller (the hub, the participants use case). Keeping
/// both constants here means the two can never disagree about what a valid code looks like.
/// </summary>
public static class RoomCodeExtensions
{
    /// <summary>Crockford's Base32 alphabet: digits and letters excluding I, L, O, U.</summary>
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public const int Length = 6;

    /// <summary>
    /// Trims, upper-cases and folds confusable glyphs (I/l to 1, o to 0) so that visually
    /// ambiguous input matches the code it was meant to be. Must run before
    /// <see cref="IsWellFormedRoomCode"/>: normalization is what makes legitimate input
    /// shape-valid in the first place.
    /// </summary>
    public static string NormalizeRoomCode(this string? code)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;

        return code
            .Trim()
            .ToUpperInvariant()
            .Replace('I', '1')
            .Replace('L', '1')
            .Replace('O', '0');
    }

    /// <summary>
    /// True only for a code already in canonical shape: exactly <see cref="Length"/> characters,
    /// every one drawn from <see cref="Alphabet"/>. Call after <see cref="NormalizeRoomCode"/>,
    /// never before.
    /// </summary>
    public static bool IsWellFormedRoomCode(this string? code) =>
        code is { Length: Length } && code.All(character => Alphabet.Contains(character));
}
