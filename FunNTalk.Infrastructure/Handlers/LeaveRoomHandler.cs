using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class LeaveRoomHandler(IHubContext<CommunicationHub> hubContext, IChatRoomRepository chatRoomRepository) : IRequestHandler<LeaveRoomCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(LeaveRoomCommand request, CancellationToken cancellationToken)
    {
        var user = _chatRoomRepository.RemoveUserFromRoom(request.RoomName, request.ConnectionId);
        if (user == null) return;

        await _hubContext.Groups.RemoveFromGroupAsync(request.ConnectionId, request.RoomName, cancellationToken);
        var userDto = new UserDto(user.Username, user.ConnectionId);

        await _hubContext.Clients.Group(request.RoomName).SendAsync("UserLeft", userDto, cancellationToken: cancellationToken);
    }
}