using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.AccessPolicies.Dtos;
using algo.Domain.Identity.Policies;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace algo.Application.Features.AccessPolicies.Commands.CreateAccessPolicy;

public sealed class CreateAccessPolicyCommandHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator,
    IAccessPolicyConditionParser conditionParser,
    IAccessPolicyMetadataProvider metadataProvider,
    ICurrentUserService currentUser) : IRequestHandler<CreateAccessPolicyCommand, AccessPolicyAdminDto>
{
    public async Task<AccessPolicyAdminDto> Handle(
        CreateAccessPolicyCommand request,
        CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.AccessPolicies,
            AccessPolicyActions.Create,
            cancellationToken);

        var resource = request.Resource.Trim();
        var action = request.Action.Trim();
        var subjectKey = request.SubjectKey.Trim();
        var description = request.Description?.Trim();

        if (!string.Equals(resource, AccessPolicyResources.Wildcard, StringComparison.Ordinal)
            && !metadataProvider.TryGetMetadata(resource, out _))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(request.Resource), $"Unknown resource '{resource}'."),
            });
        }

        if (!string.IsNullOrWhiteSpace(request.ConditionJson))
        {
            try
            {
                var ast = conditionParser.Parse(request.ConditionJson);
                var validateResource = string.Equals(
                    resource,
                    AccessPolicyResources.Wildcard,
                    StringComparison.Ordinal)
                    ? AccessPolicyResources.Users
                    : resource;
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

        var entity = new AccessPolicy
        {
            Id = Guid.NewGuid(),
            Resource = resource,
            Action = action,
            Effect = request.Effect,
            SubjectType = request.SubjectType,
            SubjectKey = subjectKey,
            ConditionJson = request.ConditionJson,
            Priority = request.Priority,
            IsEnabled = request.IsEnabled,
            Description = description,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            CreatedByUserId = currentUser.UserId,
            UpdatedByUserId = currentUser.UserId,
        };

        db.AccessPolicies.Add(entity);
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
