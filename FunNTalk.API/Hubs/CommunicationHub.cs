using FunNTalk.Domain.Entities;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using FunNTalk.Domain.DTOs;

namespace FunNTalk.API.Hubs;

public class CommunicationHub(IMediator mediator, ILogger<CommunicationHub> logger) : Hub
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<CommunicationHub> _logger = logger;

    public async Task JoinRoom(string roomName, string username)
    {
        _logger.LogInformation("user {username} joins to room {roomName}", username, roomName);
        var connectionId = Context.ConnectionId;
        var user = new UserEntity(username, connectionId, roomName);
        await _mediator.Send(new JoinRoomCommand(roomName, user));
    }

    public async Task SendMessage(string message)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("ConnectionId {connectionId} sends message", connectionId);
        var command = new SendMessageCommand(connectionId, message);
        await _mediator.Send(command);
    }

    public async Task LeaveRoom()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} leaves room", connectionId);
        var command = new LeaveRoomCommand(connectionId);
        await _mediator.Send(command);
    }

    public async Task SendOffer(string targetConnectionId, WebRtcDto offer)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {ConnectionId} sends offer to {TargetConnectionId}", connectionId, targetConnectionId);
        var command = new SendOfferCommand(connectionId, targetConnectionId, offer);
        await _mediator.Send(command);
    }

    public async Task SendAnswer(string targetConnectionId, WebRtcDto answer)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} sends answer to {TargetConnectionId}", connectionId, targetConnectionId);
        var command = new SendAnswerCommand(connectionId, targetConnectionId, answer);
        await _mediator.Send(command);
    }

    public async Task SendICECandidate(string targetConnectionId, string candidate)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} sends ice candidate to room", connectionId);
        var command = new SendICECandidateCommand(connectionId, targetConnectionId, candidate);
        await _mediator.Send(command);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Disconnected {connectionId}", connectionId);
        var command = new DisconnectedUserCommand(connectionId);
        await _mediator.Send(command);
        await base.OnDisconnectedAsync(exception);
    }
}
