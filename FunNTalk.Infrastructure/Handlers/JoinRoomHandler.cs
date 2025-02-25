using FunNTalk.API.Hubs;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FunNTalk.Infrastructure.Handlers;

public class JoinRoomHandler(IHubContext<ChatHub> hubContext) : IRequestHandler<JoinRoomCommand>
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;

    public async Task Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        await _hubContext.Groups.AddToGroupAsync(request.User.ConnectionId, request.RoomName, cancellationToken);
        await _hubContext.Clients.Group(request.RoomName).SendAsync("UserJoined", request.User, cancellationToken: cancellationToken);
    }
}
