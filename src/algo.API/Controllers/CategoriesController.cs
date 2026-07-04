using algo.Application.Features.Categories.Commands.CreateCategory;
using algo.Application.Features.Categories.Commands.DeleteCategory;
using algo.Application.Features.Categories.Commands.RestoreCategory;
using algo.Application.Features.Categories.Commands.UpdateCategory;
using algo.Application.Features.Categories.Dtos;
using algo.Application.Features.Categories.Queries.GetAllCategories;
using algo.Application.Features.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

public sealed class CategoriesController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoryDto>> List(
        [FromQuery] bool includeTrashed,
        [FromQuery] bool onlyTrashed,
        CancellationToken cancellationToken) =>
        mediator.Send(new GetAllCategoriesQuery(includeTrashed, onlyTrashed), cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryDetailsDto>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CategoryDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDetailsDto>> Update(
        int id,
        [FromBody] UpdateCategoryRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateCategoryCommand(id, body.Name, body.Description, body.ImageUrl), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPatch("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new RestoreCategoryCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}

public sealed record UpdateCategoryRequest(string Name, string? Description, string? ImageUrl);
