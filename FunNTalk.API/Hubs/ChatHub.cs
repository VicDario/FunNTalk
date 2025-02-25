using FunNTalk.Domain.Entities;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
namespace FunNTalk.API.Hubs;

public class ChatHub(IMediator mediator, ILogger<ChatHub> logger) : Hub
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger<ChatHub> _logger = logger;

    public async Task JoinRoom(string roomName, string username)
    {
        _logger.LogInformation("Join to room {roomName}, user {username}", roomName, username);
        var connectionId = Context.ConnectionId;
        var user = new UserEntity(username, connectionId);
        await _mediator.Send(new JoinRoomCommand(roomName, user));
    }

    public async Task SendMessage(string roomName, string message)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("ConnectionId {connectionId} send message to room {roomName}", connectionId, roomName);
        var command = new SendMessageCommand(connectionId, roomName, message);
        await _mediator.Send(command);
    }

    public async Task LeaveRoom(string roomName)
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("Connection {connectionId} leaves room {roomName}", connectionId, roomName);
        var command = new LeaveRoomCommand(connectionId, connectionId);
        await _mediator.Send(command);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        //var connectionId = Context.ConnectionId;

        await base.OnDisconnectedAsync(exception);
    }
}
