using System.Text.Json;

namespace algo.Application.Features.Roles.Dtos;

public sealed record RoleDetailsDto(
    string Id,
    string Name,
    string? NormalizedName,
    int UserCount,
    DateTimeOffset? TrashedAt,
    DateTimeOffset? TrashExpiresAt,
    DateTimeOffset? DeletedAt,
    JsonElement? CustomFields);
