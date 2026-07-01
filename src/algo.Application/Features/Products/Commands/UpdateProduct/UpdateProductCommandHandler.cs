using algo.Application.Abstractions;
using algo.Application.Abstractions.Persistence;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Products.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Products,
            AccessPolicyActions.Update,
            cancellationToken);

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product is null)
            return null;

        var category = await db.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(UpdateProductCommand.CategoryId), "Category was not found."),
            });
        }

        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.PriceUsd = request.PriceUsd;
        product.PriceSyp = request.PriceSyp;
        product.DiscountedPriceUsd = request.DiscountedPriceUsd;
        product.DiscountedPriceSyp = request.DiscountedPriceSyp;
        product.ExternalGameId = string.IsNullOrWhiteSpace(request.ExternalGameId)
            ? null
            : request.ExternalGameId.Trim();
        product.Provider = string.IsNullOrWhiteSpace(request.Provider)
            ? null
            : request.Provider.Trim();
        product.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl)
            ? null
            : request.ImageUrl.Trim();
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new ProductDto(
            product.Id,
            product.Name,
            product.CategoryId,
            category.Name,
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
}
