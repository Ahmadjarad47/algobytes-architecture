namespace algo.Application.Features.Users.Dtos;

public sealed record UserDetailsDto(
    string UserId,
    string? Email,
    string? UserName,
    string DisplayName,
    string? PhoneNumber,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    bool IsActive,
    bool IsLocked,
    DateTimeOffset? LockoutEnd,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);
