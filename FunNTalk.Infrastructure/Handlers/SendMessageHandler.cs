using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendMessageHandler(IHubContext<CommunicationHub> hubContext, ILogger<SendMessageHandler> logger, IChatRoomRepository chatRoomRepository) : IRequestHandler<SendMessageCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly ILogger<SendMessageHandler> _logger = logger;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending message {message} to room {roomName}", request.Message, request.RoomName);
        var user = _chatRoomRepository.GetUserFromRoom(request.RoomName, request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found in room {roomName}", request.RoomName);
            return;
        }

        var group = _hubContext.Clients.Group(request.RoomName);
        var userDto = UserDto.FromEntity(user);
        var message = new MessageDto(DateTime.Now, userDto, request.Message);
        await group.SendAsync("ReceiveMessage", message, cancellationToken: cancellationToken);
    }
}
