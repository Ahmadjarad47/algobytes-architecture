using algo.Application.Features.ErrorLogs.Dtos;
using MediatR;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogById;

public sealed record GetErrorLogByIdQuery(long Id) : IRequest<ErrorLogDto?>;
