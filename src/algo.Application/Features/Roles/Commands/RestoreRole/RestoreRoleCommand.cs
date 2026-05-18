using MediatR;

namespace algo.Application.Features.Roles.Commands.RestoreRole;

public sealed record RestoreRoleCommand(string Id) : IRequest<bool>;
