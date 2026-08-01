using System.Text.Json.Serialization;

namespace FunNTalk.Domain.DTOs;

/// <summary>
/// Mirrors the browser's RTCIceServer shape. The property names must serialize as
/// "urls", "username" and "credential" — the browser accepts nothing else.
/// Username and credential are omitted for STUN entries.
/// </summary>
public record IceServerDto(string Urls)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Credential { get; init; }
}
