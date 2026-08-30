using FunNTalk.Domain.Entities;

namespace FunNTalk.Domain.Repositories;

public interface IChatRoomRepository
{
    /// <summary>
    /// Snapshot of the room's participants, or null when the room does not exist.
    /// Callers get a copy: the live list is only mutated behind the repository.
    /// </summary>
    IReadOnlyList<UserEntity>? GetParticipants(string roomName);

    /// <summary>The only insertion path. False when the code is already live — never overwrites.</summary>
    bool TryCreateRoom(string roomName);

    /// <summary>
    /// False when the room does not exist; the caller must not create one as a side effect of
    /// joining. Clears the room's vacancy stamp on success.
    /// </summary>
    bool AddUserToRoom(string roomName, UserEntity user);
    UserEntity? RemoveUserFromRoom(string roomName, string connectionId);
    UserEntity? GetUserFromRoom(string roomName, string connectionId);
    UserEntity? GetUser(string connectionId);

    /// <summary>Reclaims every room vacant for at least <paramref name="vacancyTtl"/>. Returns the count reclaimed.</summary>
    int ReapVacantRooms(TimeSpan vacancyTtl);
}
