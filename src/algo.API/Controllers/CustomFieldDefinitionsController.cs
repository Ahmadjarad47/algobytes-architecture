using algo.Application.Features.CustomFields.Commands.CreateCustomFieldDefinition;
using algo.Application.Features.CustomFields.Commands.DeleteCustomFieldDefinition;
using algo.Application.Features.CustomFields.Commands.UpdateCustomFieldDefinition;
using algo.Application.Features.CustomFields.Dtos;
using algo.Application.Features.CustomFields.Queries.ListCustomFieldDefinitions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[Authorize]
[Route("api/v1/custom-field-definitions")]
public sealed class CustomFieldDefinitionsController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CustomFieldDefinitionDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CustomFieldDefinitionDto>> List(
        [FromQuery] string entity,
        CancellationToken cancellationToken) =>
        mediator.Send(new ListCustomFieldDefinitionsQuery(entity), cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(CustomFieldDefinitionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CustomFieldDefinitionDto>> Create(
        [FromBody] CreateCustomFieldDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(List), new { entity = created.Entity }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CustomFieldDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomFieldDefinitionDto>> Update(
        Guid id,
        [FromBody] UpdateCustomFieldDefinitionRequest body,
        CancellationToken cancellationToken)
    {
        var updated = await mediator.Send(body.ToCommand(id), cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteCustomFieldDefinitionCommand(id), cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
