using algo.Application.Features.Categories.Dtos;
using MediatR;

namespace algo.Application.Features.Categories.Queries.GetCategoryById;

public sealed record GetCategoryByIdQuery(int Id) : IRequest<CategoryDetailsDto?>;
