using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Products.Dtos;
using algo.Domain.Catalog.Entities;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Create,
            cancellationToken);

        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(CreateProductCommand.CategoryId), "Category was not found."),
            });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            PriceUsd = request.PriceUsd,
            PriceSyp = request.PriceSyp,
            DiscountedPriceUsd = request.DiscountedPriceUsd,
            DiscountedPriceSyp = request.DiscountedPriceSyp,
            ExternalGameId = string.IsNullOrWhiteSpace(request.ExternalGameId)
                ? null
                : request.ExternalGameId.Trim(),
            Provider = string.IsNullOrWhiteSpace(request.Provider)
                ? null
                : request.Provider.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
                ? null
                : request.ImageUrl.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);

        return Map(product, category.Name);
    }

    private static ProductDto Map(Product product, string categoryName) =>
        new(
            product.Id,
            product.Name,
            product.CategoryId,
            categoryName,
            product.PriceUsd,
            product.PriceSyp,
            product.DiscountedPriceUsd,
            product.DiscountedPriceSyp,
            product.ExternalGameId,
            product.Provider,
            product.ImageUrl,
            product.CreatedAt,
            product.UpdatedAt);
}
