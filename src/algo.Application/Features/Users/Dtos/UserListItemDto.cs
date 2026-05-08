namespace algo.Application.Features.Users.Dtos;

public sealed record UserListItemDto(
    string Id,
    string? Email,
    string? UserName,
    string DisplayName,
    string? PhoneNumber,
    bool IsActive,
    bool IsLocked,
    bool EmailConfirmed,
    bool PhoneNumberConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);
