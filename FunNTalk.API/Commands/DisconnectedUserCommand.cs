using MediatR;

namespace FunNTalk.API.Commands;

public record DisconnectedUserCommand(string ConnectionId) : IRequest;