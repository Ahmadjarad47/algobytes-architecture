using algo.Application.Common.Identity;
using algo.Application.Features.Roles.Commands.CreateRole;
using algo.Application.Features.Roles.Commands.DeleteRole;
using algo.Application.Features.Roles.Commands.RestoreRole;
using algo.Application.Features.Roles.Commands.UpdateRole;
using algo.Application.Features.Roles.Dtos;
using algo.Application.Features.Roles.Queries.GetRoleById;
using algo.Application.Features.Roles.Queries.GetRoles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class RolesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RoleDto>> List(
        [FromQuery] bool includeTrashed,
        [FromQuery] bool onlyTrashed,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetRolesQuery(includeTrashed, onlyTrashed), cancellationToken);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailsDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleDetailsDto>> Create(
        [FromBody] CreateRoleCommand command,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoleDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailsDto>> Update(
        string id,
        [FromBody] UpdateRoleRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateRoleCommand(id, body.Name, body.CustomFields), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteRoleCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(string id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new RestoreRoleCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
