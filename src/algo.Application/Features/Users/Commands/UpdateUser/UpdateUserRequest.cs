namespace algo.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserRequest(
    string? DisplayName,
    string? PhoneNumber,
    string? UserName,
    bool? IsActive,
    bool? EmailConfirmed);
