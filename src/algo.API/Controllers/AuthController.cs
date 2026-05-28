using algo.API.Security;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Auth.Commands.ForgotPassword;
using algo.Application.Features.Auth.Commands.Login;
using algo.Application.Features.Auth.Commands.Logout;
using algo.Application.Features.Auth.Commands.RefreshToken;
using algo.Application.Features.Auth.Commands.Register;
using algo.Application.Features.Auth.Commands.ResendOtp;
using algo.Application.Features.Auth.Commands.ResetPassword;
using algo.Application.Features.Auth.Commands.VerifyOtp;
using algo.Application.Features.CustomFields.Dtos;
using algo.Application.Features.CustomFields.Queries.ListCustomFieldDefinitions;
using algo.Application.Features.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpGet("registration-fields")]
    [ProducesResponseType(typeof(IReadOnlyList<CustomFieldDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CustomFieldDefinitionDto>> RegistrationFields(CancellationToken cancellationToken)
    {
        var definitions = await mediator.Send(
            new ListCustomFieldDefinitionsQuery(CustomFieldEntities.Users),
            cancellationToken);

        return definitions
            .Where(definition => definition.VisibleInForm)
            .ToArray();
    }

    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtp)]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthLogin)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    public Task<LoginResponseDto> Login(
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
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtp)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public Task<AuthResponseDto> VerifyOtp(
        [FromBody] VerifyOtpCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("resend-otp")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthOtp)]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> ResendOtp(
        [FromBody] ResendOtpCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthPasswordReset)]
    [ProducesResponseType(typeof(OtpVerificationDto), StatusCodes.Status200OK)]
    public Task<OtpVerificationDto> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthPasswordReset)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting(RateLimitPolicyNames.AuthRefreshToken)]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    public Task<AuthResponseDto> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);
}
