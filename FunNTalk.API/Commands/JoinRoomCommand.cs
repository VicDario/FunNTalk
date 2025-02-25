using FunNTalk.Domain.Entities;
using MediatR;

namespace FunNTalk.API.Commands;

public record JoinRoomCommand(string RoomName, UserEntity User) : IRequest;