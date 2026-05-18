using algo.Application.Features.CustomFields.Commands.CreateCustomFieldDefinition;
using algo.Application.Features.CustomFields.Commands.DeleteCustomFieldDefinition;
using algo.Application.Features.CustomFields.Commands.UpdateCustomFieldDefinition;
using algo.Application.Features.CustomFields.Dtos;
using algo.Application.Features.CustomFields.Queries.ListCustomFieldDefinitions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[ApiController]
[Authorize]
[Route("api/custom-field-definitions")]
public sealed class CustomFieldDefinitionsController(IMediator mediator) : ControllerBase
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
        [FromBody] UpdateCustomFieldDefinitionBody body,
        CancellationToken cancellationToken)
    {
        var updated = await mediator.Send(
            new UpdateCustomFieldDefinitionCommand(
                id,
                body.Label,
                body.Type,
                body.Required,
                body.Searchable,
                body.Filterable,
                body.Sortable,
                body.VisibleInTable,
                body.VisibleInForm,
                body.VisibleInDetails,
                body.Options,
                body.DefaultValue,
                body.Validation),
            cancellationToken);

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

public sealed record UpdateCustomFieldDefinitionBody(
    string Label,
    algo.Domain.CustomFields.CustomFieldType Type,
    bool Required,
    bool Searchable,
    bool Filterable,
    bool Sortable,
    bool VisibleInTable,
    bool VisibleInForm,
    bool VisibleInDetails,
    System.Text.Json.JsonElement? Options,
    System.Text.Json.JsonElement? DefaultValue,
    System.Text.Json.JsonElement? Validation);
