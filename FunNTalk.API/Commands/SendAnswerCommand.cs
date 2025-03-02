using MediatR;

namespace FunNTalk.API.Commands;

public record SendAnswerCommand(string RoomName, string ConnectionId, string TargetConnectionId, string Answer) : IRequest;