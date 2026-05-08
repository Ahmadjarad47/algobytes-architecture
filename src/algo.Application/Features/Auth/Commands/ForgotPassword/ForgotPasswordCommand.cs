using algo.Application.Features.Auth.Dtos;
using MediatR;

namespace algo.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<OtpVerificationDto>;
