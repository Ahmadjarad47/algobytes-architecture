using MediatR;

namespace algo.Application.Features.Sessions.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid Id, bool ConfirmCurrentSession = false) : IRequest<bool>;
