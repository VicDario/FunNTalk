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
public class LeaveRoomHandlerTests
{
    private const string Room = "room-a";
    private const string Connection = "connection-1";

    private readonly HubContextHarness _hub = new();
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly LeaveRoomHandler _handler;

    public LeaveRoomHandlerTests()
    {
        _handler = new LeaveRoomHandler(_hub.HubContext, _repository);
    }

    [TestMethod]
    public async Task Handle_RemovesTheUserFromTheRepositoryAndFromTheSignalRGroup()
    {
        _repository.GetUser(Connection).Returns(new UserEntity("alice", Connection, Room));
        _hub.StubGroup(Room);

        await _handler.Handle(new LeaveRoomCommand(Connection), CancellationToken.None);

        _repository.Received(1).RemoveUserFromRoom(Room, Connection);
        await _hub.Groups.Received(1).RemoveFromGroupAsync(Connection, Room, Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_BroadcastsUserLeftWithTheLeavingConnectionId()
    {
        _repository.GetUser(Connection).Returns(new UserEntity("alice", Connection, Room));
        var group = _hub.StubGroup(Room);

        await _handler.Handle(new LeaveRoomCommand(Connection), CancellationToken.None);

        await group.Received(1).SendCoreAsync(
            "UserLeft",
            Arg.Is<object?[]>(args => args.Length == 1 && ((UserDto)args[0]!) == new UserDto("alice", Connection)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_UnknownConnectionId_DoesNothing()
    {
        _repository.GetUser(Connection).Returns((UserEntity?)null);
        var group = _hub.StubGroup(Room);

        await _handler.Handle(new LeaveRoomCommand(Connection), CancellationToken.None);

        _repository.DidNotReceive().RemoveUserFromRoom(Arg.Any<string>(), Arg.Any<string>());
        await _hub.Groups.DidNotReceive().RemoveFromGroupAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await group.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }
}
