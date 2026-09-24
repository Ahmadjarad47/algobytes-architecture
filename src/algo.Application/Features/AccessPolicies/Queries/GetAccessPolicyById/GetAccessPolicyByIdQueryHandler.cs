using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Queries.GetAccessPolicyById;

public sealed class GetAccessPolicyByIdQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<GetAccessPolicyByIdQuery, AccessPolicyAdminDto?>
{
    public async Task<AccessPolicyAdminDto?> Handle(GetAccessPolicyByIdQuery request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Read,
            cancellationToken);

        var query = db.AccessPolicies
            .IgnoreQueryFilters()
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
                p.TrashedAt,
                p.TrashExpiresAt,
                p.DeletedAt,
                JsonDocumentHelpers.CloneToElement(p.CustomFields),
                p.CreatedByUserId,
                p.UpdatedByUserId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
