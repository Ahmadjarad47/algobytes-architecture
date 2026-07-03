using algo.Application.Abstractions;
using algo.Application.Common.CustomFields;
using algo.Application.Features.Shop.Products.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Shop.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.Id == request.Id)
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
