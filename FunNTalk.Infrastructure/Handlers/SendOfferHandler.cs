using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using FunNTalk.Domain.DTOs;
using FunNTalk.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendOfferHandler(IHubContext<CommunicationHub> hubContext, ILogger<SendOfferHandler> logger, IChatRoomRepository chatRoomRepository)
    : IRequestHandler<SendOfferCommand>
{
    private readonly IHubContext<CommunicationHub> _hubContext = hubContext;
    private readonly ILogger<SendOfferHandler> _logger = logger;
    private readonly IChatRoomRepository _chatRoomRepository = chatRoomRepository;

    public async Task Handle(SendOfferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending WebRTC offer from {ConnectionId}", request.ConnectionId);

        var user = _chatRoomRepository.GetUser(request.ConnectionId);
        if (user == null)
        {
            _logger.LogError("User not found -> {ConnectionId}", request.ConnectionId);
            return;
        }

        var userDto = UserDto.FromEntity(user);
        var signalDto = new WebRtcSignalDto(userDto, request.Offer);

        var group = _hubContext.Clients.GroupExcept(user.Room, request.ConnectionId);
        await group.SendAsync("ReceiveOffer", signalDto, cancellationToken);
    }
}
