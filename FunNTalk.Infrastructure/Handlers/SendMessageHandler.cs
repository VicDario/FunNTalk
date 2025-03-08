using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendMessageHandler(IHubContext<CommunicationHub> hubContext, ILogger<SendMessageHandler> logger, IChatRoomRepository chatRoomRepository)
    : IRequestHandler<SendMessageCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly ILogger<SendMessageHandler> _logger = logger;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ConnectionId} sends message", request.ConnectionId);
        var user = _chatRoomRepository.GetUser(request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found -> {ConnectionId}", request.ConnectionId);
            return;
        }

        var group = _hubContext.Clients.Group(user.Room);
        var userDto = UserDto.FromEntity(user);
        var message = new MessageDto(DateTime.Now, userDto, request.Message);
        await group.SendAsync("ReceiveMessage", message, cancellationToken: cancellationToken);
    }
}
