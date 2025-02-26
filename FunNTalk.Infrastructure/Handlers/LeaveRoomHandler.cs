using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FunNTalk.Infrastructure.Handlers;

public class LeaveRoomHandler(IHubContext<ChatHub> hubContext, IChatRoomRepository chatRoomRepository) : IRequestHandler<LeaveRoomCommand>
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        _chatRoomRepository.RemoveUserFromRoom(request.RoomName, request.ConnectionId);
        await _hubContext.Groups.RemoveFromGroupAsync(request.ConnectionId, request.RoomName, cancellationToken);
        await _hubContext.Clients.Group(request.RoomName).SendAsync("UserLeft", "User Left", cancellationToken: cancellationToken);
    }
}