using FunNTalk.API.Commands;
using FunNTalk.API.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FunNTalk.Infrastructure.Handlers;

public sealed class SendMessageHandler(IHubContext<ChatHub> hubContext, ILogger<SendMessageHandler> logger) : IRequestHandler<SendMessageCommand>
{
    private readonly IHubContext<ChatHub> _hubContext = hubContext;
    private readonly ILogger<SendMessageHandler> _logger = logger;
    public async Task Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending message {message} to room {roomName}", request.Message, request.RoomName);
        var group = _hubContext.Clients.Group(request.RoomName);
        await group.SendAsync("ReceiveMessage", request.ConnectionId, request.Message, cancellationToken: cancellationToken);
    }
}
