using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendAnswerHandler(IHubContext<CommunicationHub> hubContext, ILogger<SendAnswerHandler> logger, IChatRoomRepository chatRoomRepository)
    : IRequestHandler<SendAnswerCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly ILogger<SendAnswerHandler> _logger = logger;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(SendAnswerCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending WebRTC Answer from {connectionId} to {targetConnectionId}", request.ConnectionId, request.TargetConnectionId);

        var user = _chatRoomRepository.GetUserFromRoom(request.RoomName, request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found in room {RoomName}", request.RoomName);
            return;
        }

        var userDto = new UserDto(user.Username, user.ConnectionId);
        var signalDto = new WebRtcSignalDto(userDto, request.Answer);

        await _hubContext.Clients.Client(request.TargetConnectionId).SendAsync("ReceiveAnswer", signalDto, cancellationToken);
    }
}
