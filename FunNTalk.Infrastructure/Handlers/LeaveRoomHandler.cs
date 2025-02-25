using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FunNTalk.Infrastructure.Handlers;

public class LeaveRoomHandler(IHubContext<ChatHub> hubContext) : IRequestHandler<LeaveRoomCommand>
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;
    public async Task Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(request.ConnectionId, request.RoomName, cancellationToken);
        await _hubContext.Clients.Group(request.RoomName).SendAsync("UserLeft", "User Left", cancellationToken: cancellationToken);
    }
}