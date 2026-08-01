using FunNTalk.Domain.Entities;
using FunNTalk.Infrastructure.Repositories;

namespace FunNTalk.Tests.Infrastructure.Repositories;

[TestClass]
public class ChatRoomRepositoryTests
{
    private const string Room = "room-a";

    private readonly ChatRoomRepository _repository = new();

    [TestMethod]
    public void AddUserToRoom_CreatesTheRoomAndRegistersTheParticipant()
    {
        var user = new UserEntity("alice", "connection-1", Room);

        _repository.AddUserToRoom(Room, user);

        var participants = _repository.GetParticipants(Room);
        Assert.IsNotNull(participants);
        Assert.AreSame(user, Assert.ContainsSingle(participants));
    }

    [TestMethod]
    public void GetParticipants_ReturnsNull_ForAnUnknownRoom()
    {
        Assert.IsNull(_repository.GetParticipants("nowhere"));
    }

    [TestMethod]
    public void GetParticipants_MutatingTheReturnedList_DoesNotAffectTheRepository()
    {
        var alice = new UserEntity("alice", "connection-1", Room);
        _repository.AddUserToRoom(Room, alice);

        var snapshot = _repository.GetParticipants(Room);
        Assert.IsNotNull(snapshot);
        TryMutate(snapshot, new UserEntity("intruder", "connection-9", Room));

        var participants = _repository.GetParticipants(Room);
        Assert.IsNotNull(participants);
        Assert.AreSame(alice, Assert.ContainsSingle(participants));
    }

    [TestMethod]
    public void GetParticipants_ReturnsASnapshot_SoLaterRoomChangesDoNotLeakIntoIt()
    {
        var alice = new UserEntity("alice", "connection-1", Room);
        _repository.AddUserToRoom(Room, alice);

        var snapshot = _repository.GetParticipants(Room);

        _repository.AddUserToRoom(Room, new UserEntity("bob", "connection-2", Room));
        _repository.RemoveUserFromRoom(Room, "connection-1");

        Assert.IsNotNull(snapshot);
        Assert.AreSame(alice, Assert.ContainsSingle(snapshot));
    }

    [TestMethod]
    public void GetParticipants_ReturnsAFreshSnapshotEachCall()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));

        Assert.AreNotSame(_repository.GetParticipants(Room), _repository.GetParticipants(Room));
    }

    [TestMethod]
    public void AddUserToRoom_WithTheSameConnectionIdTwice_DoesNotDuplicateTheParticipant()
    {
        var first = new UserEntity("alice", "connection-1", Room);
        var rejoin = new UserEntity("alice", "connection-1", Room);

        _repository.AddUserToRoom(Room, first);
        _repository.AddUserToRoom(Room, rejoin);

        var participants = _repository.GetParticipants(Room);
        Assert.IsNotNull(participants);
        Assert.ContainsSingle(participants);
    }

    [TestMethod]
    public void AddUserToRoom_WithTheSameConnectionIdTwice_KeepsTheLatestRegistrationResolvable()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));
        var rejoin = new UserEntity("alice", "connection-1", Room);

        _repository.AddUserToRoom(Room, rejoin);

        Assert.AreSame(rejoin, _repository.GetUser("connection-1"));
    }

    [TestMethod]
    public void RemoveUserFromRoom_RemovingTheLastParticipant_RemovesTheRoom()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));

        _repository.RemoveUserFromRoom(Room, "connection-1");

        Assert.IsNull(_repository.GetParticipants(Room));
    }

    [TestMethod]
    public void RemoveUserFromRoom_KeepsTheRoomWhileOtherParticipantsRemain()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));
        _repository.AddUserToRoom(Room, new UserEntity("bob", "connection-2", Room));

        _repository.RemoveUserFromRoom(Room, "connection-1");

        var participants = _repository.GetParticipants(Room);
        Assert.IsNotNull(participants);
        Assert.AreEqual("bob", Assert.ContainsSingle(participants).Username);
    }

    [TestMethod]
    public void RemoveUserFromRoom_ReturnsTheRemovedUser()
    {
        var user = new UserEntity("alice", "connection-1", Room);
        _repository.AddUserToRoom(Room, user);

        Assert.AreSame(user, _repository.RemoveUserFromRoom(Room, "connection-1"));
    }

    [TestMethod]
    public void RemoveUserFromRoom_ReturnsNull_ForAnUnknownConnectionId()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));

        Assert.IsNull(_repository.RemoveUserFromRoom(Room, "connection-unknown"));
    }

    [TestMethod]
    public void RemoveUserFromRoom_ReturnsNull_ForAnUnknownRoom()
    {
        Assert.IsNull(_repository.RemoveUserFromRoom("nowhere", "connection-1"));
    }

    [TestMethod]
    public void GetUser_FindsAUserByConnectionIdAcrossRooms()
    {
        var alice = new UserEntity("alice", "connection-1", "room-a");
        var bob = new UserEntity("bob", "connection-2", "room-b");
        _repository.AddUserToRoom("room-a", alice);
        _repository.AddUserToRoom("room-b", bob);

        Assert.AreSame(alice, _repository.GetUser("connection-1"));
        Assert.AreSame(bob, _repository.GetUser("connection-2"));
    }

    [TestMethod]
    public void GetUser_ReturnsNull_ForAnUnknownConnectionId()
    {
        Assert.IsNull(_repository.GetUser("connection-unknown"));
    }

    [TestMethod]
    public void GetUser_ReturnsNull_AfterTheUserWasRemoved()
    {
        _repository.AddUserToRoom(Room, new UserEntity("alice", "connection-1", Room));

        _repository.RemoveUserFromRoom(Room, "connection-1");

        Assert.IsNull(_repository.GetUser("connection-1"));
    }

    [TestMethod]
    public void GetUserFromRoom_ResolvesOnlyWithinTheGivenRoom()
    {
        var alice = new UserEntity("alice", "connection-1", "room-a");
        _repository.AddUserToRoom("room-a", alice);

        Assert.AreSame(alice, _repository.GetUserFromRoom("room-a", "connection-1"));
        Assert.IsNull(_repository.GetUserFromRoom("room-b", "connection-1"));
        Assert.IsNull(_repository.GetUserFromRoom("nowhere", "connection-1"));
    }

    [TestMethod]
    public void ParallelAddAndRemove_DoNotThrowAndLeaveConsistentState()
    {
        const int connections = 200;

        Parallel.For(0, connections, index =>
        {
            var connectionId = $"connection-{index}";
            _repository.AddUserToRoom(Room, new UserEntity($"user-{index}", connectionId, Room));
            _repository.GetParticipants(Room);
            _repository.GetUser(connectionId);

            // Half of the connections leave again, so adds and removes interleave on the same room.
            if (index % 2 == 0) _repository.RemoveUserFromRoom(Room, connectionId);
        });

        var survivors = _repository.GetParticipants(Room);
        Assert.IsNotNull(survivors);
        Assert.AreEqual(connections / 2, survivors.Count);
        Assert.AreEqual(survivors.Count, survivors.Select(participant => participant.ConnectionId).Distinct().Count());

        foreach (var survivor in survivors)
        {
            Assert.AreSame(survivor, _repository.GetUser(survivor.ConnectionId));
        }

        for (var index = 0; index < connections; index += 2)
        {
            Assert.IsNull(_repository.GetUser($"connection-{index}"));
        }
    }

    /// <summary>
    /// The snapshot's concrete type is an implementation detail — a list, an array or a read-only
    /// wrapper. Whatever it is, writing through it must not reach the repository, and a type that
    /// refuses the write outright satisfies that just as well.
    /// </summary>
    private static void TryMutate(IReadOnlyList<UserEntity> snapshot, UserEntity intruder)
    {
        try
        {
            if (snapshot is ICollection<UserEntity> collection) collection.Add(intruder);
            if (snapshot is IList<UserEntity> { Count: > 0 } list) list[0] = intruder;
        }
        catch (NotSupportedException)
        {
        }
    }
}
