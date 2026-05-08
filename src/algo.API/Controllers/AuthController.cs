using algo.Application.Features.Auth.Commands.ForgotPassword;
using algo.Application.Features.Auth.Commands.Login;
using algo.Application.Features.Auth.Commands.Logout;
using algo.Application.Features.Auth.Commands.RefreshToken;
using algo.Application.Features.Auth.Commands.Register;
using algo.Application.Features.Auth.Commands.ResendOtp;
using algo.Application.Features.Auth.Commands.ResetPassword;
using algo.Application.Features.Auth.Commands.VerifyOtp;
using algo.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public Task<AuthResponseDto> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand? command,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command ?? new LogoutCommand(null), cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public Task<AuthResponseDto> VerifyOtp(
        [FromBody] VerifyOtpCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("resend-otp")]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> ResendOtp(
        [FromBody] ResendOtpCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public Task<AuthResponseDto> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);
}
