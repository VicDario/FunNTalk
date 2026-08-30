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
public class SendAnswerHandlerTests
{
    private const string Room = "room-a";
    private const string Sender = "connection-sender";
    private const string Target = "connection-target";

    private readonly HubContextHarness _hub = new();
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly SendAnswerHandler _handler;

    public SendAnswerHandlerTests()
    {
        _handler = new SendAnswerHandler(
            _hub.HubContext, Substitute.For<ILogger<SendAnswerHandler>>(), _repository);
    }

    [TestMethod]
    public async Task Handle_RelaysTheAnswerToTheTargetConnectionOnly()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("bob", Sender, Room));
        var target = _hub.StubClient(Target);
        var answer = new WebRtcDto("v=0", "answer");

        await _handler.Handle(new SendAnswerCommand(Sender, Target, answer), CancellationToken.None);

        await target.Received(1).SendCoreAsync(
            "ReceiveAnswer",
            Arg.Is<object?[]>(args => args.Length == 1 && ((WebRtcSignalDto)args[0]!).Data == answer),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_PutsTheSenderInThePayload_NotTheTarget()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("bob", Sender, Room));
        var target = _hub.StubClient(Target);

        await _handler.Handle(
            new SendAnswerCommand(Sender, Target, new WebRtcDto("v=0", "answer")), CancellationToken.None);

        await target.Received(1).SendCoreAsync(
            "ReceiveAnswer",
            Arg.Is<object?[]>(args => ((WebRtcSignalDto)args[0]!).User == new UserDto("bob", Sender)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_UnknownSender_SendsNothing()
    {
        _repository.GetUser(Sender).Returns((UserEntity?)null);
        var target = _hub.StubClient(Target);

        await _handler.Handle(
            new SendAnswerCommand(Sender, Target, new WebRtcDto("v=0", "answer")), CancellationToken.None);

        await target.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }
}
