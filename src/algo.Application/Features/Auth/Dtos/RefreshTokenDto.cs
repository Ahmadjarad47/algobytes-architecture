namespace algo.Application.Features.Auth.Dtos;

public sealed record RefreshTokenDto(string Token, DateTimeOffset ExpiresAtUtc);
