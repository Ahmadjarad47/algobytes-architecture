namespace algo.Application.Features.Auth.Dtos;

public sealed record TotpChallengeDto(
    bool RequiresTwoFactor,
    bool SetupRequired,
    string? SetupKey,
    string? SetupUri,
    string Message);
