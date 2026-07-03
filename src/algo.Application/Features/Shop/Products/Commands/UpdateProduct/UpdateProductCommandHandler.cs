using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Products.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IApplicationDbContext db,
    CustomFieldValueValidator customFieldValueValidator)
    : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
        {
            return null;
        }

        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.CategoryId), "Category was not found."),
            });
        }

        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        product.Price = request.Price;
        product.DiscountedPrice = request.DiscountedPrice;
        product.CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
            CustomFieldEntities.Products,
            JsonDocumentHelpers.CloneToElement(request.CustomFields),
            cancellationToken);
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? null
            : request.ImageUrl.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return new ProductDto(
            product.Id,
            product.Name,
            product.CategoryId,
            category.Name,
            product.CurrencyCode,
            product.Price,
            product.DiscountedPrice,
            JsonDocumentHelpers.CloneToElement(product.CustomFields),
            product.ImageUrl,
            product.CreatedAt);
    }
}
