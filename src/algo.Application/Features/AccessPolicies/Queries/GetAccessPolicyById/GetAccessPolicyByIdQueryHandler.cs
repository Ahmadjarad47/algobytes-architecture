using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyById;

public sealed class GetAccessPolicyByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetAccessPolicyByIdQuery, AccessPolicyAdminDto?>
{
    public async Task<AccessPolicyAdminDto?> Handle(GetAccessPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = db.AccessPolicies
            .AsNoTracking()
            .Where(p => p.Id == request.Id && p.DeletedAt == null);

        return await query
            .Select(p => new AccessPolicyAdminDto(
                p.Id,
                p.Resource,
                p.Action,
                p.Effect,
                p.SubjectType,
                p.SubjectKey,
                p.ConditionJson,
                p.Priority,
                p.IsEnabled,
                p.Description,
                p.ValidFrom,
                p.ValidTo,
                p.DeletedAt,
                p.CreatedByUserId,
                p.UpdatedByUserId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
