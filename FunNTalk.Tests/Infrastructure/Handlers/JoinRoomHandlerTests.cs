using FunNTalk.API.Commands;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Handlers;
using FunNTalk.Tests.TestDoubles;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.Handlers;

[TestClass]
public class JoinRoomHandlerTests
{
    private const string Room = "room-a";
    private const string NewConnection = "connection-new";
    private const string StaleConnection = "connection-stale";

    private readonly HubContextHarness _hub = new();
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly JoinRoomHandler _handler;

    public JoinRoomHandlerTests()
    {
        _handler = new JoinRoomHandler(_hub.HubContext, _repository);
    }

    /// <summary>
    /// NSubstitute returns false for an unconfigured bool method, so every test below that
    /// exercises a successful join needs this arranged — except the rejection test, which
    /// overrides it with its own <c>.Returns(false)</c>.
    /// </summary>
    [TestInitialize]
    public void GivenTheRoomAcceptsTheJoiningUser() =>
        _repository.AddUserToRoom(Room, Arg.Any<UserEntity>()).Returns(true);

    [TestMethod]
    public async Task Handle_AddsTheUserToTheRepositoryAndToTheSignalRGroup()
    {
        var user = new UserEntity("alice", NewConnection, Room);
        GivenRoomContains();
        _hub.StubGroupExcept(Room);

        await _handler.Handle(new JoinRoomCommand(Room, user), CancellationToken.None);

        _repository.Received(1).AddUserToRoom(Room, user);
        await _hub.Groups.Received(1).AddToGroupAsync(NewConnection, Room, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_BroadcastsUserJoinedToTheGroupExceptTheJoiningConnection()
    {
        var user = new UserEntity("alice", NewConnection, Room);
        GivenRoomContains();
        var groupExcept = _hub.StubGroupExcept(Room);

        await _handler.Handle(new JoinRoomCommand(Room, user), CancellationToken.None);

        await groupExcept.Received(1).SendCoreAsync(
            "UserJoined",
            Arg.Is<object?[]>(args => args.Length == 1
                && args[0] is UserDto
                && ((UserDto)args[0]!).Username == "alice"
                && ((UserDto)args[0]!).ConnectionId == NewConnection),
            Arg.Any<CancellationToken>());

        _hub.Clients.Received(1).GroupExcept(
            Room,
            Arg.Is<IReadOnlyList<string>>(excluded => excluded.Contains(NewConnection)));
    }

    [TestMethod]
    public async Task Handle_JoinIntoARoomThatDoesNotExist_ThrowsAndCreatesNothing()
    {
        var user = new UserEntity("alice", NewConnection, Room);
        _repository.AddUserToRoom(Room, user).Returns(false);
        var groupExcept = _hub.StubGroupExcept(Room);

        await Assert.ThrowsExactlyAsync<HubException>(
            () => _handler.Handle(new JoinRoomCommand(Room, user), CancellationToken.None));

        _repository.DidNotReceive().TryCreateRoom(Arg.Any<string>());
        await groupExcept.DidNotReceive().SendCoreAsync(
            "UserJoined", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_RejoinWithTheSameUsernameAndANewConnection_EvictsTheStaleConnection()
    {
        GivenRoomContains(new UserEntity("alice", StaleConnection, Room));
        _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        _repository.Received(1).RemoveUserFromRoom(Room, StaleConnection);
        await _hub.Groups.Received(1).RemoveFromGroupAsync(StaleConnection, Room, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_RejoinWithTheSameUsername_BroadcastsUserLeftCarryingTheStaleConnectionId()
    {
        GivenRoomContains(new UserEntity("alice", StaleConnection, Room));
        var group = _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        await group.Received(1).SendCoreAsync(
            "UserLeft",
            Arg.Is<object?[]>(args => args.Length == 1
                && args[0] is UserDto
                && ((UserDto)args[0]!).ConnectionId == StaleConnection),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_RejoinWithTheSameUsername_AnnouncesUserLeftBeforeUserJoined()
    {
        GivenRoomContains(new UserEntity("alice", StaleConnection, Room));
        var group = _hub.StubGroup(Room);
        var groupExcept = _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        Received.InOrder(() =>
        {
            group.SendCoreAsync("UserLeft", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
            groupExcept.SendCoreAsync("UserJoined", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        });
    }

    [TestMethod]
    public async Task Handle_RejoinWithTheSameUsername_RemovesTheStaleConnectionBeforeAddingTheNewOne()
    {
        GivenRoomContains(new UserEntity("alice", StaleConnection, Room));
        _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);
        var user = new UserEntity("alice", NewConnection, Room);

        await _handler.Handle(new JoinRoomCommand(Room, user), CancellationToken.None);

        Received.InOrder(() =>
        {
            _repository.RemoveUserFromRoom(Room, StaleConnection);
            _repository.AddUserToRoom(Room, user);
        });
    }

    [TestMethod]
    public async Task Handle_DoesNotEvictADifferentUsernameInTheSameRoom()
    {
        GivenRoomContains(new UserEntity("bob", "connection-bob", Room));
        var group = _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        _repository.DidNotReceive().RemoveUserFromRoom(Arg.Any<string>(), Arg.Any<string>());
        await _hub.Groups.DidNotReceive().RemoveFromGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await group.DidNotReceive().SendCoreAsync("UserLeft", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_DoesNotEvictTheSameUsernameWhenTheConnectionIdIsUnchanged()
    {
        GivenRoomContains(new UserEntity("alice", NewConnection, Room));
        _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        _repository.DidNotReceive().RemoveUserFromRoom(Arg.Any<string>(), Arg.Any<string>());
    }

    [TestMethod]
    public async Task Handle_RejoinWithSeveralStaleConnections_EvictsEveryOneOfThem()
    {
        GivenRoomContains(
            new UserEntity("alice", "connection-stale-1", Room),
            new UserEntity("alice", "connection-stale-2", Room),
            new UserEntity("bob", "connection-bob", Room));
        _hub.StubGroup(Room);
        _hub.StubGroupExcept(Room);

        await HandleRejoinAsync();

        _repository.Received(1).RemoveUserFromRoom(Room, "connection-stale-1");
        _repository.Received(1).RemoveUserFromRoom(Room, "connection-stale-2");
        _repository.DidNotReceive().RemoveUserFromRoom(Room, "connection-bob");
    }

    private void GivenRoomContains(params UserEntity[] participants) =>
        _repository.GetParticipants(Room).Returns(participants);

    private Task HandleRejoinAsync() =>
        _handler.Handle(new JoinRoomCommand(Room, new UserEntity("alice", NewConnection, Room)), CancellationToken.None);
}
