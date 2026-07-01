using algo.Application.Features.Categories.Dtos;
using MediatR;

namespace algo.Application.Features.Categories.Queries.GetAllCategories;

public sealed record GetAllCategoriesQuery(
    bool IncludeTrashed = false,
    bool OnlyTrashed = false) : IRequest<IReadOnlyList<CategoryDto>>;
