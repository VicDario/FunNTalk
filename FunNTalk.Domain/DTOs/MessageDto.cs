namespace FunNTalk.Domain.DTOs;

/// <summary>
/// <paramref name="Id"/> is server generated so the client can track messages by identity
/// instead of by array index or timestamp — two messages can land in the same millisecond.
/// <paramref name="Timestamp"/> must serialize as ISO 8601; the client renders it with
/// Angular's date pipe, which breaks on a custom format or a Unix number.
/// </summary>
public record MessageDto(Guid Id, DateTimeOffset Timestamp, UserDto User, string Message);
