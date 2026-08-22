namespace FunNTalk.Domain.Entities;

public class ChatRoomEntity(string name)
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; init; } = name;
    public List<UserEntity> Participants { get; set; } = [];

    /// <summary>
    /// When the room became empty of participants, or when it was created if nobody has ever
    /// joined. Null while at least one participant is present. The reaper reclaims the room once
    /// this timestamp is old enough; joining clears it back to null.
    /// </summary>
    public DateTimeOffset? VacantSince { get; set; }
}
