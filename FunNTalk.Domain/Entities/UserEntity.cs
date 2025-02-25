namespace FunNTalk.Domain.Entities;

public class UserEntity(string username, string connectionId)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Username { get; init; } = username;
    public string ConnectionId { get; init; } = connectionId;
}