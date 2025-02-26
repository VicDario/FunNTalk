namespace FunNTalk.Domain.DTOs;

public class MessageDto(DateTime timestamp, string user, string message)
{
    public DateTime Timestamp { get; init; } = timestamp;
    public string User { get; init; } = user;
    public string Message { get; init; } = message;
}
