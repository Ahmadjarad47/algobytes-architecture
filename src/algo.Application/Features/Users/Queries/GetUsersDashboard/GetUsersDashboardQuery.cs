using algo.Application.Features.Users.Dtos;
using MediatR;

namespace algo.Application.Features.Users.Queries.GetUsersDashboard;

public sealed record GetUsersDashboardQuery : IRequest<UserDashboardStatsDto>;
