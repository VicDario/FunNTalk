using MediatR;

namespace FunNTalk.API.Commands;

public record SendMessageCommand(string ConnectionId, string Message) : IRequest;