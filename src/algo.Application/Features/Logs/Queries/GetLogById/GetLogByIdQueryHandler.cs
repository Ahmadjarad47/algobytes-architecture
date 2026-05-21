using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Logs.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Logs.Queries.GetLogById;

public sealed class GetLogByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetLogByIdQuery, ApplicationLogDto?>
{
    public async Task<ApplicationLogDto?> Handle(GetLogByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Logs,
            AccessPolicyActions.Read,
            cancellationToken);

        var row = await db.ApplicationLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        return row?.Adapt<ApplicationLogDto>();
    }
}
