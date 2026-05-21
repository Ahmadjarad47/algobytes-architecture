using algo.Application.Features.Logs.Dtos;
using MediatR;

namespace algo.Application.Features.Logs.Queries.GetLogById;

public sealed record GetLogByIdQuery(long Id) : IRequest<ApplicationLogDto?>;
