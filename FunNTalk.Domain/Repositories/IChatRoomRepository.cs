using FunNTalk.Domain.Entities;

namespace FunNTalk.Domain.Repositories;

public interface IChatRoomRepository
{
    ChatRoomEntity? GetRoom(string roomName);
    void AddUserToRoom(string roomName, UserEntity user);
    void RemoveUserFromRoom(string roomName, string connectionId);
    UserEntity? GetUserFromRoom(string roomName, string connectionId);
}
