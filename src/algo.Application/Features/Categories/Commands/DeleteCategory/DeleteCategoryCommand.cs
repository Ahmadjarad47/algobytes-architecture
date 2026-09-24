using MediatR;

namespace algo.Application.Features.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(int Id) : IRequest<bool>;
