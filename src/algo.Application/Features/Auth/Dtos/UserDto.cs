namespace algo.Application.Features.Auth.Dtos;

public sealed record UserDto(
    string UserId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
