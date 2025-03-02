using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendICECandidateHandler(IHubContext<CommunicationHub> hubContext, ILogger<SendICECandidateHandler> logger, IChatRoomRepository chatRoomRepository)
    : IRequestHandler<SendICECandidateCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly ILogger<SendICECandidateHandler> _logger = logger;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(SendICECandidateCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending WebRTC ICE candidate from {ConnectionId} in room {RoomName}", request.ConnectionId, request.RoomName);
        var user = _chatRoomRepository.GetUserFromRoom(request.RoomName, request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found in room {RoomName}", request.RoomName);
            return;
        }

        var userDto = new UserDto(user.Username, user.ConnectionId);
        var signalDto = new WebRtcSignalDto(userDto, request.Candidate);

        await _hubContext.Clients.GroupExcept(request.RoomName, request.ConnectionId).SendAsync("ReceiveICECandidate", signalDto, cancellationToken);
    }
}
