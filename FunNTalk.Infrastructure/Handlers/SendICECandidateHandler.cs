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
        _logger.LogInformation(
            "Sending WebRTC ICE candidate from {ConnectionId} to {TargetConnectionId}",
            request.ConnectionId,
            request.TargetConnectionId);

        var user = _chatRoomRepository.GetUser(request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found -> {ConnectionId}", request.ConnectionId);
            return;
        }

        var userDto = UserDto.FromEntity(user);

        // The candidate arrives already JSON-stringified and is relayed as an opaque string:
        // the receiving client runs JSON.parse on it, which throws if it is re-serialized as
        // an object, and every candidate is silently lost.
        var signalDto = new WebRtcCandidate(userDto, request.Candidate);

        var client = _hubContext.Clients.Client(request.TargetConnectionId);
        await client.SendAsync("ReceiveICECandidate", signalDto, cancellationToken);
    }
}
