using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Application.Features.Users.Dtos;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace algo.Application.Features.Users.Queries.GetUserPermissionGraph;

public sealed class GetUserPermissionGraphQueryHandler(
    IApplicationDbContext db,
    IAccessPolicyEvaluator accessPolicyEvaluator)
    : IRequestHandler<GetUserPermissionGraphQuery, UserPermissionGraphDto>
{
    public async Task<UserPermissionGraphDto> Handle(GetUserPermissionGraphQuery request, CancellationToken cancellationToken)
    {
        await accessPolicyEvaluator.EnsureResourceActionAllowedAsync(
            db,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        IQueryable<ApplicationUser> scoped = db.Users.AsNoTracking();
        scoped = await accessPolicyEvaluator.ApplyAsync(
            scoped,
            AccessPolicyResources.Users,
            AccessPolicyActions.Read,
            cancellationToken);

        var key = request.UserId.Trim();
        var user = await scoped
            .Where(u => u.Id == key || u.UserName == key || (u.Email != null && u.Email == key))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(GetUserPermissionGraphQuery.UserId), "User was not found."),
            });
        }

        var roles = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == user.Id
            select new { r.Name, r.NormalizedName })
            .ToListAsync(cancellationToken);

        var roleNames = roles
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => x.Name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var normalizedRoleNames = roles
            .Where(x => !string.IsNullOrWhiteSpace(x.NormalizedName))
            .Select(x => x.NormalizedName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var policies = await db.AccessPolicies
            .AsNoTracking()
            .Where(p =>
                p.IsEnabled &&
                p.DeletedAt == null &&
                (
                    (p.SubjectType == AccessPolicySubjectType.Role &&
                     (roleNames.Contains(p.SubjectKey) || normalizedRoleNames.Contains(p.SubjectKey)))
                    ||
                    (p.SubjectType == AccessPolicySubjectType.User && p.SubjectKey == user.Id)
                ))
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.Resource)
            .ThenBy(p => p.Action)
            .ToListAsync(cancellationToken);

        var nodes = new List<UserPermissionGraphNodeDto>();
        var edges = new List<UserPermissionGraphEdgeDto>();

        var userLabel = string.IsNullOrWhiteSpace(user.DisplayName)
            ? (user.Email ?? user.Id)
            : user.DisplayName;
        var userNodeId = $"user:{user.Id}";
        nodes.Add(new UserPermissionGraphNodeDto(userNodeId, "user", userLabel));

        var roleNodeIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roleNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var roleNodeId = $"role:{role}";
            roleNodeIds[role] = roleNodeId;
            nodes.Add(new UserPermissionGraphNodeDto(roleNodeId, "role", role));
            edges.Add(new UserPermissionGraphEdgeDto(userNodeId, roleNodeId, "hasRole"));
        }

        var resourceNodeIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var policy in policies)
        {
            var policyNodeId = $"policy:{policy.Id}";
            nodes.Add(new UserPermissionGraphNodeDto(
                policyNodeId,
                "policy",
                $"{policy.Resource}:{policy.Action}",
                policy.Resource,
                policy.Action,
                policy.Effect.ToString(),
                policy.ConditionJson,
                policy.Priority,
                policy.IsEnabled));

            if (policy.SubjectType == AccessPolicySubjectType.User)
            {
                edges.Add(new UserPermissionGraphEdgeDto(userNodeId, policyNodeId, "hasPolicy"));
            }
            else
            {
                var roleNodeId = roleNames
                    .FirstOrDefault(r => r.Equals(policy.SubjectKey, StringComparison.OrdinalIgnoreCase))
                    ?? roles.FirstOrDefault(r => string.Equals(r.NormalizedName, policy.SubjectKey, StringComparison.OrdinalIgnoreCase))?.Name;

                if (!string.IsNullOrWhiteSpace(roleNodeId) && roleNodeIds.TryGetValue(roleNodeId, out var roleEdgeFrom))
                    edges.Add(new UserPermissionGraphEdgeDto(roleEdgeFrom, policyNodeId, "hasPolicy"));
            }

            var resourceKey = policy.Resource;
            if (!resourceNodeIds.TryGetValue(resourceKey, out var resourceNodeId))
            {
                resourceNodeId = $"resource:{resourceKey}";
                resourceNodeIds[resourceKey] = resourceNodeId;
                nodes.Add(new UserPermissionGraphNodeDto(resourceNodeId, "resource", resourceKey));
            }

            edges.Add(new UserPermissionGraphEdgeDto(policyNodeId, resourceNodeId, "grants"));
        }

        return new UserPermissionGraphDto(user.Id, nodes, edges);
    }
}
