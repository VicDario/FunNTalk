using FunNTalk.Domain.Entities;

namespace FunNTalk.Domain.Repositories;

public interface IChatRoomRepository
{
    /// <summary>
    /// Snapshot of the room's participants, or null when the room does not exist.
    /// Callers get a copy: the live list is only mutated behind the repository.
    /// </summary>
    IReadOnlyList<UserEntity>? GetParticipants(string roomName);

    void AddUserToRoom(string roomName, UserEntity user);
    UserEntity? RemoveUserFromRoom(string roomName, string connectionId);
    UserEntity? GetUserFromRoom(string roomName, string connectionId);
    UserEntity? GetUser(string connectionId);
}
