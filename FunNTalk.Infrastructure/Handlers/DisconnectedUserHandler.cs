using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace FunNTalk.Infrastructure.Handlers;

class DisconnectedUserHandler(IHubContext<CommunicationHub> hubContext, IChatRoomRepository chatRoomRepository) : IRequestHandler<DisconnectedUserCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(DisconnectedUserCommand request, CancellationToken cancellationToken)
    {
        var user = _chatRoomRepository.GetUser(request.ConnectionId);
        if (user == null) return;
        _chatRoomRepository.RemoveUserFromRoom(user.Room, request.ConnectionId);

        await _hubContext.Groups.RemoveFromGroupAsync(request.ConnectionId, user.Room, cancellationToken);
        var userDto = UserDto.FromEntity(user);
        await _hubContext.Clients.Group(user.Room).SendAsync("UserLeft", userDto, cancellationToken: cancellationToken);
    }
}
