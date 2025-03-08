using MediatR;

namespace FunNTalk.API.Commands;

public record SendICECandidateCommand(string ConnectionId, string TargetConnectionId, string Candidate) : IRequest;