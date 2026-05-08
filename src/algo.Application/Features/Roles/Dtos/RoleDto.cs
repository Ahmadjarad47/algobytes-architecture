namespace algo.Application.Features.Roles.Dtos;

public sealed record RoleDto(
    string Id,
    string Name,
    string? NormalizedName);
