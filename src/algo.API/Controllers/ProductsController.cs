using System.Text.Json;
using algo.Application.Features.Shop.Products.Commands.CreateProduct;
using algo.Application.Features.Shop.Products.Commands.DeleteProduct;
using algo.Application.Features.Shop.Products.Commands.UpdateProduct;
using algo.Application.Features.Shop.Products.Dtos;
using algo.Application.Features.Shop.Products.Queries.GetAllProducts;
using algo.Application.Features.Shop.Products.Queries.GetProductById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace algo.API.Controllers;

[Authorize]
public sealed class ProductsController(IMediator mediator) : BaseController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ProductDto>> List(CancellationToken cancellationToken) =>
        mediator.Send(new GetAllProductsQuery(), cancellationToken);

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        [FromBody] UpdateProductRequest body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateProductCommand(
                id,
                body.Name,
                body.CategoryId,
                body.CurrencyCode,
                body.Price,
                body.DiscountedPrice,
                body.CustomFields,
                body.ImageUrl),
            cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return ok ? NoContent() : NotFound();
    }

}

public sealed record UpdateProductRequest(
    string Name,
    int CategoryId,
    string CurrencyCode,
    decimal Price,
    decimal? DiscountedPrice,
    JsonDocument? CustomFields,
    string? ImageUrl);
