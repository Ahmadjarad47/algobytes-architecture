using algo.Application.Features.Auth.Dtos;
using MediatR;

namespace algo.Application.Features.Auth.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string Email, string Code) : IRequest<AuthResponseDto>;
