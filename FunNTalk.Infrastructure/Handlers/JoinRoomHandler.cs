using FunNTalk.API.Hubs;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.DTOs;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class JoinRoomHandler(IHubContext<CommunicationHub> hubContext, IChatRoomRepository chatRoomRepository) : IRequestHandler<JoinRoomCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        _chatRoomRepository.AddUserToRoom(request.RoomName, request.User);
        await _hubContext.Groups.AddToGroupAsync(request.User.ConnectionId, request.RoomName, cancellationToken);

        var userDto = UserDto.FromEntity(request.User);
        await _hubContext.Clients.Group(request.RoomName).SendAsync("UserJoined", userDto, cancellationToken: cancellationToken);
    }
}
