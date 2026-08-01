using FunNTalk.API.Commands;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;
using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Handlers;
using FunNTalk.Tests.TestDoubles;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FunNTalk.Tests.Infrastructure.Handlers;

[TestClass]
public class SendMessageHandlerTests
{
    private const string Room = "room-a";
    private const string Sender = "connection-sender";

    private readonly HubContextHarness _hub = new();
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly SendMessageHandler _handler;

    public SendMessageHandlerTests()
    {
        _handler = new SendMessageHandler(
            _hub.HubContext, Substitute.For<ILogger<SendMessageHandler>>(), _repository);
    }

    [TestMethod]
    public async Task Handle_BroadcastsReceiveMessageToTheSendersRoom()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        var group = _hub.StubGroup(Room);

        await _handler.Handle(new SendMessageCommand(Sender, "hello"), CancellationToken.None);

        await group.Received(1).SendCoreAsync(
            "ReceiveMessage", Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_StampsTheMessageWithAnIdentityAUtcTimestampAndTheSender()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        var group = _hub.StubGroup(Room);
        var before = DateTimeOffset.UtcNow;

        await _handler.Handle(new SendMessageCommand(Sender, "hello"), CancellationToken.None);

        var message = CapturedMessage(group);
        Assert.AreNotEqual(Guid.Empty, message.Id);
        Assert.AreEqual(TimeSpan.Zero, message.Timestamp.Offset);
        Assert.IsTrue(
            message.Timestamp >= before && message.Timestamp <= DateTimeOffset.UtcNow,
            $"The timestamp {message.Timestamp:O} was stamped outside the window the handler ran in.");
        Assert.AreEqual(new UserDto("alice", Sender), message.User);
        Assert.AreEqual("hello", message.Message);
    }

    [TestMethod]
    public async Task Handle_GivesEveryMessageItsOwnIdentity()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        var group = _hub.StubGroup(Room);

        await _handler.Handle(new SendMessageCommand(Sender, "hello"), CancellationToken.None);
        await _handler.Handle(new SendMessageCommand(Sender, "hello"), CancellationToken.None);

        var ids = group.ReceivedCalls()
            .Select(call => ((MessageDto)((object?[])call.GetArguments()[1]!)[0]!).Id)
            .ToList();

        Assert.AreEqual(2, ids.Count);
        Assert.AreEqual(2, ids.Distinct().Count());
    }

    [TestMethod]
    public async Task Handle_UnknownSender_SendsNothing()
    {
        _repository.GetUser(Sender).Returns((UserEntity?)null);
        var group = _hub.StubGroup(Room);

        await _handler.Handle(new SendMessageCommand(Sender, "hello"), CancellationToken.None);

        await group.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    private static MessageDto CapturedMessage(IClientProxy group)
    {
        var call = Assert.ContainsSingle(group.ReceivedCalls());
        var arguments = call.GetArguments();
        Assert.AreEqual("ReceiveMessage", arguments[0]);
        return Assert.IsInstanceOfType<MessageDto>(((object?[])arguments[1]!)[0]);
    }
}
