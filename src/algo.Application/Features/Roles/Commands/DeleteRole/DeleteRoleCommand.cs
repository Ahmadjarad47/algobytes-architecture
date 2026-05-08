using MediatR;

namespace algo.Application.Features.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(string Id) : IRequest<bool>;
