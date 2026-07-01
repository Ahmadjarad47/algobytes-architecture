namespace algo.Application.Features.Categories.Dtos;

public sealed record CategoryDto(
    int Id,
    string Name,
    string? Description,
    int ProductCount);

public sealed record CategoryDetailsDto(
    int Id,
    string Name,
    string? Description,
    int ProductCount);
