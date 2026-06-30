using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.ErrorLogs.Dtos;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.ErrorLogs.Queries.GetErrorLogById;

public sealed class GetErrorLogByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetErrorLogByIdQuery, ErrorLogDto?>
{
    public async Task<ErrorLogDto?> Handle(GetErrorLogByIdQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.ErrorLogs,
            AccessPolicyActions.Read,
            cancellationToken);

        var row = await db.ErrorLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        return row?.Adapt<ErrorLogDto>();
    }
}
