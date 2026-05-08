using algo.Application.Common.Pagination;
using algo.Application.Features.Users.Commands.ActivateUser;
using algo.Application.Features.Users.Commands.AssignRoles;
using algo.Application.Features.Users.Commands.ChangeUserPassword;
using algo.Application.Features.Users.Commands.ConfirmUserEmail;
using algo.Application.Features.Users.Commands.CreateUser;
using algo.Application.Features.Users.Commands.DeactivateUser;
using algo.Application.Features.Users.Commands.DeleteUser;
using algo.Application.Features.Users.Commands.LockUser;
using algo.Application.Features.Users.Commands.RemoveRoles;
using algo.Application.Features.Users.Commands.UnlockUser;
using algo.Application.Features.Users.Commands.UpdateUser;
using algo.Application.Features.Users.Dtos;
using algo.Application.Features.Users.Queries.GetUserById;
using algo.Application.Features.Users.Queries.GetUserRoles;
using algo.Application.Features.Users.Queries.GetUsers;
using algo.Application.Features.Users.Queries.GetUsersDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public Task<PaginatedResult<UserListItemDto>> List(
        [FromQuery] GetUsersQueryParameters parameters,
        CancellationToken cancellationToken) =>
        mediator.Send(parameters.ToQuery(), cancellationToken);

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(UserDashboardStatsDto), StatusCodes.Status200OK)]
    public Task<UserDashboardStatsDto> Dashboard(CancellationToken cancellationToken) =>
        mediator.Send(new GetUsersDashboardQuery(), cancellationToken);

    [HttpGet("{id}/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<UserRoleDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<UserRoleDto>> GetRoles(string id, CancellationToken cancellationToken) =>
        mediator.Send(new GetUserRolesQuery(id), cancellationToken);

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDetailsDto>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetailsDto>> Update(
        string id,
        [FromBody] UpdateUserRequest body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            id,
            body.DisplayName,
            body.PhoneNumber,
            body.UserName,
            body.IsActive,
            body.EmailConfirmed);
        var result = await mediator.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Activate(string id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ActivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeactivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Lock(
        string id,
        [FromBody] LockUserRequest body,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new LockUserCommand(id, body.LockoutEnd), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlock(string id, CancellationToken cancellationToken)
    {
        await mediator.Send(new UnlockUserCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id}/confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfirmEmail(string id, CancellationToken cancellationToken)
    {
        await mediator.Send(new ConfirmUserEmailCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id}/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(
        string id,
        [FromBody] ChangeUserPasswordRequest body,
        CancellationToken cancellationToken)
    {
        await mediator.Send(
            new ChangeUserPasswordCommand(id, body.NewPassword, body.ConfirmPassword),
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignRoles(
        string id,
        [FromBody] AssignRolesRequest body,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignRolesCommand(id, body.Roles), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRoles(
        string id,
        [FromBody] RemoveRolesRequest body,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveRolesCommand(id, body.Roles), cancellationToken);
        return NoContent();
    }
}
