namespace algo.Application.Features.Categories.Dtos;

public sealed record CategoryDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int ProductCount,
    DateTimeOffset? TrashedAt,
    DateTimeOffset? TrashExpiresAt,
    DateTimeOffset? DeletedAt);

public sealed record CategoryDetailsDto(
    int Id,
    string Name,
    string? Description,
    string? ImageUrl,
    int ProductCount,
    DateTimeOffset? TrashedAt,
    DateTimeOffset? TrashExpiresAt,
    DateTimeOffset? DeletedAt);
