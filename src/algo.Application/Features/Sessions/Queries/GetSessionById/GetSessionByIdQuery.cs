using algo.Application.Features.Sessions.Dtos;
using MediatR;

namespace algo.Application.Features.Sessions.Queries.GetSessionById;

public sealed record GetSessionByIdQuery(Guid Id) : IRequest<ActiveSessionDto?>;
