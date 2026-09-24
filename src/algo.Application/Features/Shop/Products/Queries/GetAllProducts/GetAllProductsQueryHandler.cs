using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Products.Queries.GetAllProducts;

public sealed class GetAllProductsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.CategoryId,
                product.Category.Name,
                product.CurrencyCode,
                product.Price,
                product.DiscountedPrice,
                JsonDocumentHelpers.CloneToElement(product.CustomFields),
                product.ImageUrl,
                product.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
