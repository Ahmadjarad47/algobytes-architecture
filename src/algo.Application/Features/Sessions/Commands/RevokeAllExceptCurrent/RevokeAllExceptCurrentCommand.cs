using MediatR;

namespace algo.Application.Features.Sessions.Commands.RevokeAllExceptCurrent;

public sealed record RevokeAllExceptCurrentCommand(string Confirmation) : IRequest<int>;
