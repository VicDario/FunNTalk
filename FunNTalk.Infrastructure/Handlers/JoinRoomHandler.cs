using FunNTalk.API.Hubs;
using FunNTalk.API.Commands;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using FunNTalk.Domain.Repositories;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Entities;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class JoinRoomHandler(IHubContext<CommunicationHub> hubContext, IChatRoomRepository chatRoomRepository)
    : IRequestHandler<JoinRoomCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        await EvictStaleConnectionsAsync(request, cancellationToken);

        _chatRoomRepository.AddUserToRoom(request.RoomName, request.User);
        await _hubContext.Groups.AddToGroupAsync(request.User.ConnectionId, request.RoomName, cancellationToken);

        var userDto = UserDto.FromEntity(request.User);

        // Only the joining peer initiates offers. The rest receive UserJoined and wait, which is
        // what stops both sides from offering at once.
        var group = _hubContext.Clients.GroupExcept(request.RoomName, request.User.ConnectionId);
        await group.SendAsync("UserJoined", userDto, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// A SignalR reconnect issues a new connection id, and the old one may not have been reaped
    /// yet. Anything still registered under the same username is a dead peer: drop it and tell
    /// the room, so the others close the stale RTCPeerConnection instead of keeping a frozen
    /// tile, and the rejoining client is not handed a participant it can never negotiate with.
    /// </summary>
    private async Task EvictStaleConnectionsAsync(JoinRoomCommand request, CancellationToken cancellationToken)
    {
        var participants = _chatRoomRepository.GetParticipants(request.RoomName);
        if (participants is null) return;

        var staleConnections = participants
            .Where(participant => participant.Username == request.User.Username
                && participant.ConnectionId != request.User.ConnectionId)
            .ToList();

        foreach (var staleConnection in staleConnections)
        {
            await RemoveStaleConnectionAsync(request.RoomName, staleConnection, cancellationToken);
        }
    }

    private async Task RemoveStaleConnectionAsync(
        string roomName,
        UserEntity staleConnection,
        CancellationToken cancellationToken)
    {
        _chatRoomRepository.RemoveUserFromRoom(roomName, staleConnection.ConnectionId);
        await _hubContext.Groups.RemoveFromGroupAsync(staleConnection.ConnectionId, roomName, cancellationToken);

        var group = _hubContext.Clients.Group(roomName);
        await group.SendAsync("UserLeft", UserDto.FromEntity(staleConnection), cancellationToken: cancellationToken);
    }
}
