namespace FunNTalk.Domain.Entities;

public class UserEntity(string username, string connectionId, string room)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Username { get; init; } = username;
    public string ConnectionId { get; init; } = connectionId;
    public string Room { get; init; } = room;
}