namespace FunNTalk.Domain.DTOs;

public record MessageDto(DateTime Timestamp, UserDto User, string Message);
