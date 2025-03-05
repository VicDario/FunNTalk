using FunNTalk.Domain.DTOs;
using MediatR;

namespace FunNTalk.API.Commands;

public record SendOfferCommand(string RoomName, string ConnectionId, WebRtcDto Offer) : IRequest;