using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.GetUserPermissionGraph;

public sealed record GetUserPermissionGraphQuery(string UserId) : IRequest<UserPermissionGraphDto>;
