using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FunNTalk.Tests.Api;

/// <summary>
/// The hub is a thin edge: its only job is to stamp every command with the caller's connection id
/// and hand it to MediatR. Anything the caller could forge — most of all the connection id — must
/// come from the hub context, never from the client's arguments.
/// </summary>
[TestClass]
public class CommunicationHubTests
{
    private const string Caller = "connection-caller";
    private const string Target = "connection-target";
    private const string Room = "room-a";

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CommunicationHub _hub;

    public CommunicationHubTests()
    {
        var context = Substitute.For<HubCallerContext>();
        context.ConnectionId.Returns(Caller);

        _hub = new CommunicationHub(_mediator, Substitute.For<ILogger<CommunicationHub>>())
        {
            Context = context,
        };
    }

    [TestCleanup]
    public void DisposeHub() => _hub.Dispose();

    [TestMethod]
    public async Task JoinRoom_SendsAJoinRoomCommandCarryingTheCallersConnectionId()
    {
        await _hub.JoinRoom(Room, "alice");

        await _mediator.Received(1).Send(
            Arg.Is<JoinRoomCommand>(command => command.RoomName == Room
                && command.User.Username == "alice"
                && command.User.ConnectionId == Caller
                && command.User.Room == Room),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendMessage_SendsASendMessageCommandCarryingTheCallersConnectionId()
    {
        await _hub.SendMessage("hello");

        await _mediator.Received(1).Send(
            Arg.Is<SendMessageCommand>(command => command.ConnectionId == Caller && command.Message == "hello"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task LeaveRoom_SendsALeaveRoomCommandCarryingTheCallersConnectionId()
    {
        await _hub.LeaveRoom();

        await _mediator.Received(1).Send(
            Arg.Is<LeaveRoomCommand>(command => command.ConnectionId == Caller),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendOffer_SendsASendOfferCommandCarryingBothTheCallerAndTheTarget()
    {
        var offer = new WebRtcDto("v=0", "offer");

        await _hub.SendOffer(Target, offer);

        await _mediator.Received(1).Send(
            Arg.Is<SendOfferCommand>(command => command.ConnectionId == Caller
                && command.TargetConnectionId == Target
                && command.Offer == offer),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendAnswer_SendsASendAnswerCommandCarryingBothTheCallerAndTheTarget()
    {
        var answer = new WebRtcDto("v=0", "answer");

        await _hub.SendAnswer(Target, answer);

        await _mediator.Received(1).Send(
            Arg.Is<SendAnswerCommand>(command => command.ConnectionId == Caller
                && command.TargetConnectionId == Target
                && command.Answer == answer),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task SendICECandidate_SendsASendICECandidateCommandCarryingBothTheCallerAndTheTarget()
    {
        const string candidate = """{"candidate":"candidate:1 1 UDP 1 10.0.0.1 1 typ host"}""";

        await _hub.SendICECandidate(Target, candidate);

        await _mediator.Received(1).Send(
            Arg.Is<SendICECandidateCommand>(command => command.ConnectionId == Caller
                && command.TargetConnectionId == Target
                && ReferenceEquals(command.Candidate, candidate)),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task OnDisconnectedAsync_SendsADisconnectedUserCommandCarryingTheCallersConnectionId()
    {
        await _hub.OnDisconnectedAsync(null);

        await _mediator.Received(1).Send(
            Arg.Is<DisconnectedUserCommand>(command => command.ConnectionId == Caller),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task OnDisconnectedAsync_StillReportsTheDisconnectWhenTheConnectionFaulted()
    {
        await _hub.OnDisconnectedAsync(new IOException("the transport died"));

        await _mediator.Received(1).Send(
            Arg.Is<DisconnectedUserCommand>(command => command.ConnectionId == Caller),
            Arg.Any<CancellationToken>());
    }
}
