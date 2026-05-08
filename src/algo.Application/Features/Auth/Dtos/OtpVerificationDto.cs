namespace algo.Application.Features.Auth.Dtos;

public sealed record OtpVerificationDto(string Email, DateTimeOffset ExpiresAtUtc, string Message);
