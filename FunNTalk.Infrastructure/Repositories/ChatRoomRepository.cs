using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;

namespace FunNTalk.Infrastructure.Repositories;

public sealed class ChatRoomRepository : IChatRoomRepository
{
    private readonly Dictionary<string, ChatRoomEntity> _rooms = [];
    public void AddUserToRoom(string roomName, UserEntity user)
    {
        if (_rooms.TryGetValue(roomName, out var room))
            room.Participants.Add(user);
        else
            CreateRoom(roomName, user);
    }

    private void CreateRoom(string roomName, UserEntity user)
    {
        _rooms.Add(roomName, new ChatRoomEntity(roomName) { Participants = [user] });
    }

    public ChatRoomEntity? GetRoom(string roomName)
    {
        _rooms.TryGetValue(roomName, out var room);
        return room;
    }

    public void RemoveUserFromRoom(string roomName, string connectionId)
    {
        _rooms.TryGetValue(roomName, out var room);
        room?.Participants.RemoveAll(participant => participant.ConnectionId == connectionId);
    }

    public UserEntity? GetUserFromRoom(string roomName, string connectionId)
    {
        var room = GetRoom(roomName);
        return room?.Participants.FirstOrDefault(participant => participant.ConnectionId == connectionId);
    }
}
