using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;
using System.Collections.Concurrent;

namespace FunNTalk.Infrastructure.Repositories;

public sealed class ChatRoomRepository : IChatRoomRepository
{
    private readonly ConcurrentDictionary<string, ChatRoomEntity> _rooms = [];
    private readonly ConcurrentDictionary<string, UserEntity> _users = [];

    public void AddUserToRoom(string roomName, UserEntity user)
    {
        _users.TryAdd(user.ConnectionId, user);
        if (_rooms.TryGetValue(roomName, out var room))
            room.Participants.Add(user);
        else
            CreateRoom(roomName, user);
    }

    public ChatRoomEntity? GetRoom(string roomName)
    {
        _rooms.TryGetValue(roomName, out var room);
        return room;
    }

    public UserEntity? RemoveUserFromRoom(string roomName, string connectionId)
    {
        _rooms.TryGetValue(roomName, out var room);
        var user = room?.Participants.Find(participant => participant.ConnectionId == connectionId);
        if (user is null) return null;
        room?.Participants.Remove(user);
        if (room?.Participants.Count == 0)
            RemoveRoom(roomName);
        _users.TryRemove(connectionId, out _);
        return user;
    }

    public UserEntity? GetUserFromRoom(string roomName, string connectionId)
    {
        var room = GetRoom(roomName);
        return room?.Participants.FirstOrDefault(participant => participant.ConnectionId == connectionId);
    }

    public UserEntity? GetUser(string connectionId)
    {
        _users.TryGetValue(connectionId, out var user);
        return user;
    }

    private void CreateRoom(string roomName, UserEntity user)
    {
        _rooms.TryAdd(roomName, new ChatRoomEntity(roomName) { Participants = [user] });
    }

    private void RemoveRoom(string roomName)
    {
        _rooms.TryRemove(roomName, out _);
    }
}
