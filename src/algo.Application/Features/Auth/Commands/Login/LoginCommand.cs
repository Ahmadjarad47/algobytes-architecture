using algo.Application.Features.Auth.Dtos;
using MediatR;

namespace algo.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
