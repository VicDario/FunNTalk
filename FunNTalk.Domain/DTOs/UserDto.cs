using FunNTalk.Domain.Entities;

namespace FunNTalk.Domain.DTOs;

public record UserDto(string Username, string ConnectionId)
{
    public static UserDto FromEntity(UserEntity user)
    {
        return new UserDto(user.Username, user.ConnectionId);
    }
};