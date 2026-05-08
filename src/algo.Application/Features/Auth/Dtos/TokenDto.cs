namespace algo.Application.Features.Auth.Dtos;

public sealed record TokenDto(string AccessToken, DateTimeOffset AccessTokenExpiresAt, RefreshTokenDto Refresh);
