namespace FunNTalk.Domain.Entities;

public class ChatRoomEntity(string name)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = name;
    public List<UserEntity> Participants { get; set; } = [];
}
