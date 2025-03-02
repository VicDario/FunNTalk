using MediatR;

namespace FunNTalk.API.Commands;

public record SendOfferCommand(string RoomName, string ConnectionId, string Offer) : IRequest;