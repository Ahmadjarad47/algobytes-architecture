using System.Text.Json;
using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string Email,
    string UserName,
    string DisplayName,
    string? PhoneNumber,
    string Password,
    string ConfirmPassword,
    IReadOnlyList<string>? Roles,
    bool EmailConfirmed,
    bool IsActive,
    JsonElement? CustomFields) : IRequest<UserDetailsDto>;
