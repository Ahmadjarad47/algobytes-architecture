using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(string UserId) : IRequest<UserDetailsDto?>;
