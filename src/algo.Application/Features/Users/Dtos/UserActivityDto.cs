namespace algo.Application.Features.Users.Dtos;

public sealed record UserActivityDto(
    string UserId,
    string? Email,
    string DisplayName,
    DateTimeOffset OccurredAt,
    string ActivityKind);
