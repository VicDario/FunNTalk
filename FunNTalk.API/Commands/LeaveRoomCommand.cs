using MediatR;

namespace FunNTalk.API.Commands;

public record LeaveRoomCommand(string ConnectionId) : IRequest;