using FunNTalk.Domain.Entities;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

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

    public async Task SendMessage(string roomName, string message)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("ConnectionId {connectionId} sends message to room {roomName}", connectionId, roomName);
        var command = new SendMessageCommand(connectionId, roomName, message);
        await _mediator.Send(command);
    }

    public async Task LeaveRoom(string roomName)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} leaves room {roomName}", connectionId, roomName);
        var command = new LeaveRoomCommand(roomName, connectionId);
        await _mediator.Send(command);
    }

    public async Task SendOffer(string roomName, string offer)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} sends offer to room {roomName}", connectionId, roomName);
        var command = new SendOfferCommand(roomName, connectionId, offer);
        await _mediator.Send(command);
    }

    public async Task SendAnswer(string roomName, string targetConnectionId, string answer)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} sends answer to room {roomName}", connectionId, roomName);
        var command = new SendAnswerCommand(roomName, connectionId, targetConnectionId, answer);
        await _mediator.Send(command);
    }

    public async Task SendICECandidate(string roomName, string candidate)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} sends ice candidate to room {roomName}", connectionId, roomName);
        var command = new SendOfferCommand(roomName, connectionId, candidate);
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
