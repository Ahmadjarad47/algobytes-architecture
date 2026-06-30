using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Common.CustomFields;
using algo.Application.Features.AccessPolicies.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Commands.SetAccessPolicyEnabled;

public sealed class SetAccessPolicyEnabledCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IAccessPolicyAuthorizationChecker authorizationChecker)
    : IRequestHandler<SetAccessPolicyEnabledCommand, AccessPolicyAdminDto?>
{
    public async Task<AccessPolicyAdminDto?> Handle(SetAccessPolicyEnabledCommand request, CancellationToken cancellationToken)
    {
        await authorizationChecker.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Update,
            cancellationToken);

        var entity = await db.AccessPolicies
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.DeletedAt == null, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedByUserId = currentUser.UserId;
        await db.SaveChangesAsync(cancellationToken);

        return new AccessPolicyAdminDto(
            entity.Id,
            entity.Resource,
            entity.Action,
            entity.Effect,
            entity.SubjectType,
            entity.SubjectKey,
            entity.ConditionJson,
            entity.Priority,
            entity.IsEnabled,
            entity.Description,
            entity.ValidFrom,
            entity.ValidTo,
            entity.TrashedAt,
            entity.TrashExpiresAt,
            entity.DeletedAt,
            JsonDocumentHelpers.CloneToElement(entity.CustomFields),
            entity.CreatedByUserId,
            entity.UpdatedByUserId);
    }
}
