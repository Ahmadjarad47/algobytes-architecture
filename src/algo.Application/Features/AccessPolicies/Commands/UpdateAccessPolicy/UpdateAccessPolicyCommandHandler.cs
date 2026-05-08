using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.AccessPolicies.Dtos;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.AccessPolicies.Commands.UpdateAccessPolicy;

public sealed class UpdateAccessPolicyCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IAccessPolicyConditionParser conditionParser,
    IAccessPolicyMetadataProvider metadataProvider,
    ICurrentUserService currentUser) : IRequestHandler<UpdateAccessPolicyCommand, AccessPolicyAdminDto?>
{
    public async Task<AccessPolicyAdminDto?> Handle(UpdateAccessPolicyCommand request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
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

        if (!string.Equals(request.Resource, AccessPolicyResources.Wildcard, StringComparison.Ordinal)
            && !metadataProvider.TryGetMetadata(request.Resource, out _))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Resource), $"Unknown resource '{request.Resource}'."),
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ConditionJson))
        {
            try
            {
                var ast = conditionParser.Parse(request.ConditionJson);
                var validateResource = string.Equals(
                    request.Resource,
                    AccessPolicyResources.Wildcard,
                    StringComparison.Ordinal)
                    ? AccessPolicyResources.Users
                    : request.Resource;
                conditionParser.Validate(validateResource, ast, metadataProvider);
            }
            catch (AccessPolicyConditionParseException ex)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.ConditionJson), ex.Message),
                });
            }
            catch (AccessPolicyConditionValidationException ex)
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(nameof(request.ConditionJson), ex.Message),
                });
            }
        }

        entity.Resource = request.Resource.Trim();
        entity.Action = request.Action.Trim();
        entity.Effect = request.Effect;
        entity.SubjectType = request.SubjectType;
        entity.SubjectKey = request.SubjectKey.Trim();
        entity.ConditionJson = request.ConditionJson;
        entity.Priority = request.Priority;
        entity.IsEnabled = request.IsEnabled;
        entity.Description = request.Description?.Trim();
        entity.ValidFrom = request.ValidFrom;
        entity.ValidTo = request.ValidTo;
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
            entity.DeletedAt,
            entity.CreatedByUserId,
            entity.UpdatedByUserId);
    }
}
