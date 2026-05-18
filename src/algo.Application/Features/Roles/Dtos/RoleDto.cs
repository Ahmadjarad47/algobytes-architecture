using System.Text.Json;

namespace algo.Application.Features.Roles.Dtos;

public sealed record RoleDto(
    string Id,
    string Name,
    string? NormalizedName,
    DateTimeOffset? TrashedAt,
    DateTimeOffset? TrashExpiresAt,
    DateTimeOffset? DeletedAt,
    JsonElement? CustomFields);
