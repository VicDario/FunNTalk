using MediatR;

namespace FunNTalk.API.Commands;

public record LeaveRoomCommand(string RoomName, string ConnectionId) : IRequest;