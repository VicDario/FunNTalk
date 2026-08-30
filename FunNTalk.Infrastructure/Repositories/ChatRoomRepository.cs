using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;

namespace FunNTalk.Infrastructure.Repositories;

/// <summary>
/// In-memory room registry. Hub invocations run concurrently and the participant lists are
/// plain <see cref="List{T}"/>, so every read and write goes through one lock — a concurrent
/// dictionary would only protect the outer map, not the lists inside it.
/// </summary>
public sealed class ChatRoomRepository(TimeProvider timeProvider) : IChatRoomRepository
{
    private readonly Dictionary<string, ChatRoomEntity> _rooms = [];
    private readonly Dictionary<string, UserEntity> _users = [];
    private readonly object _gate = new();
    private readonly TimeProvider _timeProvider = timeProvider;

    public bool TryCreateRoom(string roomName)
    {
        lock (_gate)
        {
            if (_rooms.ContainsKey(roomName)) return false;

            _rooms[roomName] = new ChatRoomEntity(roomName) { VacantSince = _timeProvider.GetUtcNow() };
            return true;
        }
    }

    public bool AddUserToRoom(string roomName, UserEntity user)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomName, out var room)) return false;

            // A re-join is authoritative for its connection id, but it must not duplicate the
            // participant: a duplicate becomes a second video tile that nobody ever closes.
            _users[user.ConnectionId] = user;
            room.VacantSince = null;
            if (room.Participants.Exists(participant => participant.ConnectionId == user.ConnectionId)) return true;

            room.Participants.Add(user);
            return true;
        }
    }

    public IReadOnlyList<UserEntity>? GetParticipants(string roomName)
    {
        lock (_gate)
        {
            return _rooms.TryGetValue(roomName, out var room) ? [.. room.Participants] : null;
        }
    }

    public UserEntity? RemoveUserFromRoom(string roomName, string connectionId)
    {
        lock (_gate)
        {
            if (!_rooms.TryGetValue(roomName, out var room)) return null;

            var user = room.Participants.Find(participant => participant.ConnectionId == connectionId);
            if (user is null) return null;

            room.Participants.Remove(user);
            if (room.Participants.Count == 0) room.VacantSince = _timeProvider.GetUtcNow();
            _users.Remove(connectionId);

            return user;
        }
    }

    public UserEntity? GetUserFromRoom(string roomName, string connectionId)
    {
        lock (_gate)
        {
            return _rooms.TryGetValue(roomName, out var room)
                ? room.Participants.Find(participant => participant.ConnectionId == connectionId)
                : null;
        }
    }

    public UserEntity? GetUser(string connectionId)
    {
        lock (_gate)
        {
            _users.TryGetValue(connectionId, out var user);
            return user;
        }
    }

    public int ReapVacantRooms(TimeSpan vacancyTtl)
    {
        lock (_gate)
        {
            var now = _timeProvider.GetUtcNow();
            var expired = _rooms
                .Where(entry => entry.Value.VacantSince is { } vacantSince && now - vacantSince >= vacancyTtl)
                .Select(entry => entry.Key)
                .ToList();

            foreach (var roomName in expired) _rooms.Remove(roomName);

            return expired.Count;
        }
    }
}
