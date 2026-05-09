namespace algo.Application.Features.Auth.Dtos;

public sealed record LoginResponseDto(
    UserDto? User,
    TokenDto? Tokens,
    TotpChallengeDto? TotpChallenge);
