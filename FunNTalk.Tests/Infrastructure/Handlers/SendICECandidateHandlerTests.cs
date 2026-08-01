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
public class SendICECandidateHandlerTests
{
    private const string Room = "room-a";
    private const string Sender = "connection-sender";
    private const string Target = "connection-target";

    private const string Candidate =
        """{"candidate":"candidate:1 1 UDP 2130706431 10.0.0.1 54321 typ host","sdpMid":"0","sdpMLineIndex":0}""";

    private readonly HubContextHarness _hub = new();
    private readonly IChatRoomRepository _repository = Substitute.For<IChatRoomRepository>();
    private readonly SendICECandidateHandler _handler;

    public SendICECandidateHandlerTests()
    {
        _handler = new SendICECandidateHandler(
            _hub.HubContext, Substitute.For<ILogger<SendICECandidateHandler>>(), _repository);
    }

    [TestMethod]
    public async Task Handle_SendsReceiveICECandidateToTheTargetConnection()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        var target = _hub.StubClient(Target);

        await HandleAsync();

        await target.Received(1).SendCoreAsync(
            "ReceiveICECandidate",
            Arg.Is<object?[]>(args => args.Length == 1
                && ((WebRtcCandidate)args[0]!).User == new UserDto("alice", Sender)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_DoesNotBroadcastTheCandidateToTheRoom()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        _hub.StubClient(Target);
        var group = _hub.StubGroup(Room);

        await HandleAsync();

        await group.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_RelaysTheCandidateAsTheSameOpaqueString()
    {
        _repository.GetUser(Sender).Returns(new UserEntity("alice", Sender, Room));
        var target = _hub.StubClient(Target);

        await HandleAsync();

        // The receiving client runs JSON.parse on this value, so it must arrive as the very same
        // string that came in — never deserialized and re-serialized into an object.
        await target.Received(1).SendCoreAsync(
            "ReceiveICECandidate",
            Arg.Is<object?[]>(args => ReferenceEquals(((WebRtcCandidate)args[0]!).Candidate, Candidate)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Handle_UnknownSender_SendsNothing()
    {
        _repository.GetUser(Sender).Returns((UserEntity?)null);
        var target = _hub.StubClient(Target);
        var group = _hub.StubGroup(Room);

        await HandleAsync();

        await target.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
        await group.DidNotReceive().SendCoreAsync(
            Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>());
    }

    private Task HandleAsync() =>
        _handler.Handle(new SendICECandidateCommand(Sender, Target, Candidate), CancellationToken.None);
}
