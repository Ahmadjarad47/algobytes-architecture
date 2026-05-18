using System.Text.Json;

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
    DateTimeOffset? TrashedAt,
    DateTimeOffset? TrashExpiresAt,
    DateTimeOffset? DeletedAt,
    JsonElement? CustomFields,
    bool TwoFactorEnabled,
    bool TotpRequiredByAdmin,
    IReadOnlyList<string> Roles);
