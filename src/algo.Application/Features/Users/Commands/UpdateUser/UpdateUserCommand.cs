using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    string UserId,
    string? DisplayName,
    string? PhoneNumber,
    string? UserName,
    bool? IsActive,
    bool? EmailConfirmed) : IRequest<UserDetailsDto?>;
