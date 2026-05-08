using algo.Application.Features.Auth.Dtos;
using MediatR;

namespace algo.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName) : IRequest<OtpVerificationDto>;
