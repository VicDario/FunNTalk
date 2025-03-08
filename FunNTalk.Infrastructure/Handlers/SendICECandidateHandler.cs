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
        _logger.LogInformation("Sending WebRTC ICE candidate from {ConnectionId}", request.ConnectionId);
        var user = _chatRoomRepository.GetUser(request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found -> {ConnectionId}", request.ConnectionId);
            return;
        }

        var userDto = UserDto.FromEntity(user);
        var signalDto = new WebRtcCandidate(userDto, request.Candidate);

        var group = _hubContext.Clients.GroupExcept(user.Room, request.ConnectionId);
        await group.SendAsync("ReceiveICECandidate", signalDto, cancellationToken);
    }
}
