using algo.Application.Features.Auth.Dtos;
using MediatR;
using System.Text.Json;

namespace algo.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string ConfirmPassword,
    string DisplayName,
    JsonElement? CustomFields = null) : IRequest<OtpVerificationDto>;
