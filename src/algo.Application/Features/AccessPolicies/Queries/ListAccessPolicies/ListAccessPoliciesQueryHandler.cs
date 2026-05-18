using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Queries.ListAccessPolicies;

public sealed class ListAccessPoliciesQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<ListAccessPoliciesQuery, IReadOnlyList<AccessPolicyAdminDto>>
{
    public async Task<IReadOnlyList<AccessPolicyAdminDto>> Handle(
        ListAccessPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = (request.IncludeTrashed || request.OnlyTrashed
                ? db.AccessPolicies.IgnoreQueryFilters()
                : db.AccessPolicies)
            .AsNoTracking();

        if (request.OnlyTrashed)
        {
            query = query.Where(policy => policy.TrashedAt != null && policy.DeletedAt == null);
        }
        else if (!request.IncludeTrashed)
        {
            query = query.Where(policy => policy.TrashedAt == null && policy.DeletedAt == null);
        }

        return await query
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
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
                p.TrashedAt,
                p.TrashExpiresAt,
                p.DeletedAt,
                JsonDocumentHelpers.CloneToElement(p.CustomFields),
                p.CreatedByUserId,
                p.UpdatedByUserId))
            .ToListAsync(cancellationToken);
    }
}
