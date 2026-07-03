using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Products.Dtos;
using algo.Domain.Catalog.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IApplicationDbContext db,
    CustomFieldValueValidator customFieldValueValidator)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
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

        var product = new Product
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
            Price = request.Price,
            DiscountedPrice = request.DiscountedPrice,
            CustomFields = await customFieldValueValidator.ValidateAndNormalizeAsync(
                CustomFieldEntities.Products,
                JsonDocumentHelpers.CloneToElement(request.CustomFields),
                cancellationToken),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : request.ImageUrl.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Products.Add(product);
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
