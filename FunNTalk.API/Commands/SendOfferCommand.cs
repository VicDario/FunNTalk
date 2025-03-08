using FunNTalk.Domain.DTOs;
using MediatR;

namespace FunNTalk.API.Commands;

public record SendOfferCommand(string ConnectionId, WebRtcDto Offer) : IRequest;