using algo.Application.Features.AccessPolicies.Commands.CreateAccessPolicy;
using algo.Application.Features.AccessPolicies.Commands.SetAccessPolicyEnabled;
using algo.Application.Features.AccessPolicies.Commands.SoftDeleteAccessPolicy;
using algo.Application.Features.AccessPolicies.Commands.UpdateAccessPolicy;
using algo.Application.Features.AccessPolicies.Commands.ValidateAccessPolicyCondition;
using algo.Application.Features.AccessPolicies.Dtos;
using algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyById;
using algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyOptions;
using algo.Application.Features.AccessPolicies.Queries.ListAccessPolicies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class AccessPoliciesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccessPolicyAdminDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AccessPolicyAdminDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new ListAccessPoliciesQuery(), cancellationToken);

    [HttpGet("options")]
    [ProducesResponseType(typeof(AccessPolicyOptionsDto), StatusCodes.Status200OK)]
    public Task<AccessPolicyOptionsDto> Options(CancellationToken cancellationToken) =>
        mediator.Send(new GetAccessPolicyOptionsQuery(), cancellationToken);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccessPolicyAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessPolicyAdminDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccessPolicyByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccessPolicyAdminDto), StatusCodes.Status200OK)]
    public Task<AccessPolicyAdminDto> Create(
        [FromBody] CreateAccessPolicyCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccessPolicyAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessPolicyAdminDto>> Update(
        Guid id,
        [FromBody] UpdateAccessPolicyBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccessPolicyCommand(
            id,
            body.Resource,
            body.Action,
            body.Effect,
            body.SubjectType,
            body.SubjectKey,
            body.ConditionJson,
            body.Priority,
            body.IsEnabled,
            body.Description,
            body.ValidFrom,
            body.ValidTo);
        var result = await mediator.Send(command, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/enabled")]
    [ProducesResponseType(typeof(AccessPolicyAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessPolicyAdminDto>> SetEnabled(
        Guid id,
        [FromBody] SetEnabledRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetAccessPolicyEnabledCommand(id, body.IsEnabled), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new SoftDeleteAccessPolicyCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("validate-condition")]
    [ProducesResponseType(typeof(ValidateAccessPolicyConditionResultDto), StatusCodes.Status200OK)]
    public Task<ValidateAccessPolicyConditionResultDto> ValidateCondition(
        [FromBody] ValidateAccessPolicyConditionCommand command,
        CancellationToken cancellationToken) =>
        mediator.Send(command, cancellationToken);
}

public sealed record SetEnabledRequest(bool IsEnabled);
