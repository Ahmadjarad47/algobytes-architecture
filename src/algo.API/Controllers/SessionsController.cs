using algo.Application.Features.Sessions.Commands.RevokeAllExceptCurrent;
using algo.Application.Features.Sessions.Commands.RevokeSelectedSessions;
using algo.Application.Features.Sessions.Commands.RevokeSession;
using algo.Application.Features.Sessions.Commands.RevokeUserSessions;
using algo.Application.Features.Sessions.Dtos;
using algo.Application.Features.Sessions.Queries.GetSessionById;
using algo.Application.Features.Sessions.Queries.GetSessions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class SessionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ActiveSessionsResponseDto), StatusCodes.Status200OK)]
    public Task<ActiveSessionsResponseDto> List(
        [FromQuery] GetSessionsQueryParameters parameters,
        CancellationToken cancellationToken) =>
        mediator.Send(parameters.ToQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ActiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActiveSessionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var session = await mediator.Send(new GetSessionByIdQuery(id), cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        Guid id,
        [FromBody] RevokeSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var revoked = await mediator.Send(
            new RevokeSessionCommand(id, request?.ConfirmCurrentSession ?? false),
            cancellationToken);

        return revoked ? NoContent() : NotFound();
    }

    [HttpPost("users/{userId}/revoke")]
    [ProducesResponseType(typeof(RevokeCountResponse), StatusCodes.Status200OK)]
    public async Task<RevokeCountResponse> RevokeUserSessions(
        string userId,
        [FromBody] RevokeUserSessionsRequest? request,
        CancellationToken cancellationToken)
    {
        var count = await mediator.Send(
            new RevokeUserSessionsCommand(userId, request?.ConfirmCurrentUser ?? false),
            cancellationToken);

        return new RevokeCountResponse(count);
    }

    [HttpPost("revoke-selected")]
    [ProducesResponseType(typeof(RevokeCountResponse), StatusCodes.Status200OK)]
    public async Task<RevokeCountResponse> RevokeSelected(
        [FromBody] RevokeSelectedSessionsRequest request,
        CancellationToken cancellationToken)
    {
        var count = await mediator.Send(new RevokeSelectedSessionsCommand(request.Ids), cancellationToken);
        return new RevokeCountResponse(count);
    }

    [HttpPost("revoke-all-except-current")]
    [ProducesResponseType(typeof(RevokeCountResponse), StatusCodes.Status200OK)]
    public async Task<RevokeCountResponse> RevokeAllExceptCurrent(
        [FromBody] RevokeAllExceptCurrentRequest request,
        CancellationToken cancellationToken)
    {
        var count = await mediator.Send(new RevokeAllExceptCurrentCommand(request.Confirmation), cancellationToken);
        return new RevokeCountResponse(count);
    }
}
