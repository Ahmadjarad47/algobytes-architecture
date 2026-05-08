using algo.Application.Features.Auth.Dtos;
using MediatR;

namespace algo.Application.Features.Auth.Commands.ResendOtp;

public sealed record ResendOtpCommand(string Email) : IRequest<OtpVerificationDto>;
