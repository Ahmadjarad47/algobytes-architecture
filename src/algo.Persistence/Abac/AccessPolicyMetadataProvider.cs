using System.Collections.ObjectModel;
using System.Reflection;
using algo.Application.Abstractions;
using algo.Application.Common.AccessPolicy;
using algo.Domain.Identity.Entities;
using algo.Domain.Identity.Policies;
using algo.Domain.Logging.Entities;

namespace algo.Persistence.Abac;

public sealed class AccessPolicyMetadataProvider : IAccessPolicyMetadataProvider
{
    private static readonly IReadOnlyDictionary<string, AccessPolicyEntityMetadata> MetadataByResource =
        new ReadOnlyDictionary<string, AccessPolicyEntityMetadata>(
            new Dictionary<string, AccessPolicyEntityMetadata>(StringComparer.OrdinalIgnoreCase)
            {
                [AccessPolicyResources.Users] = CreateUsersMetadata(),
                [AccessPolicyResources.Roles] = CreateRolesMetadata(),
                [AccessPolicyResources.AccessPolicies] = CreateAccessPoliciesMetadata(),
                [AccessPolicyResources.Sessions] = CreateSessionsMetadata(),
                [AccessPolicyResources.Logs] = CreateLogsMetadata(),
                [AccessPolicyResources.ErrorLogs] = CreateErrorLogsMetadata(),
            });

    public IReadOnlyCollection<string> GetRegisteredResources() => MetadataByResource.Keys.ToList();

    public bool TryGetMetadata(string resource, out AccessPolicyEntityMetadata? metadata)
    {
        if (MetadataByResource.TryGetValue(resource, out var found))
        {
            metadata = found;
            return true;
        }

        metadata = null;
        return false;
    }

    private static AccessPolicyEntityMetadata CreateUsersMetadata()
    {
        var type = typeof(ApplicationUser);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(ApplicationUser.Id)),
            ["email"] = Field(type, nameof(ApplicationUser.Email)),
            ["userName"] = Field(type, nameof(ApplicationUser.UserName)),
            ["fullName"] = Field(type, nameof(ApplicationUser.DisplayName)),
            ["isActive"] = Field(type, nameof(ApplicationUser.IsActive)),
            ["emailConfirmed"] = Field(type, nameof(ApplicationUser.EmailConfirmed)),
            ["createdAt"] = Field(type, nameof(ApplicationUser.CreatedAt)),
            ["updatedAt"] = Field(type, nameof(ApplicationUser.UpdatedAt)),
            ["lastLoginAt"] = Field(type, nameof(ApplicationUser.LastLoginAt)),
            ["phoneNumberConfirmed"] = Field(type, nameof(ApplicationUser.PhoneNumberConfirmed)),
            ["createdByUserId"] = Field(type, nameof(ApplicationUser.CreatedByUserId)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyEntityMetadata CreateRolesMetadata()
    {
        var type = typeof(ApplicationRole);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(ApplicationRole.Id)),
            ["name"] = Field(type, nameof(ApplicationRole.Name)),
            ["normalizedName"] = Field(type, nameof(ApplicationRole.NormalizedName)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyEntityMetadata CreateAccessPoliciesMetadata()
    {
        var type = typeof(AccessPolicy);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(AccessPolicy.Id)),
            ["resource"] = Field(type, nameof(AccessPolicy.Resource)),
            ["action"] = Field(type, nameof(AccessPolicy.Action)),
            ["effect"] = Field(type, nameof(AccessPolicy.Effect)),
            ["subjectType"] = Field(type, nameof(AccessPolicy.SubjectType)),
            ["subjectKey"] = Field(type, nameof(AccessPolicy.SubjectKey)),
            ["priority"] = Field(type, nameof(AccessPolicy.Priority)),
            ["isEnabled"] = Field(type, nameof(AccessPolicy.IsEnabled)),
            ["createdByUserId"] = Field(type, nameof(AccessPolicy.CreatedByUserId)),
            ["updatedByUserId"] = Field(type, nameof(AccessPolicy.UpdatedByUserId)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyEntityMetadata CreateLogsMetadata()
    {
        var type = typeof(ApplicationLog);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(ApplicationLog.Id)),
            ["timestamp"] = Field(type, nameof(ApplicationLog.Timestamp)),
            ["level"] = Field(type, nameof(ApplicationLog.Level)),
            ["userId"] = Field(type, nameof(ApplicationLog.UserId)),
            ["userName"] = Field(type, nameof(ApplicationLog.UserName)),
            ["requestPath"] = Field(type, nameof(ApplicationLog.RequestPath)),
            ["requestMethod"] = Field(type, nameof(ApplicationLog.RequestMethod)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyEntityMetadata CreateSessionsMetadata()
    {
        var type = typeof(RefreshToken);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(RefreshToken.Id)),
            ["userId"] = Field(type, nameof(RefreshToken.UserId)),
            ["expiresAt"] = Field(type, nameof(RefreshToken.ExpiresAt)),
            ["createdAt"] = Field(type, nameof(RefreshToken.CreatedAt)),
            ["lastActivityAt"] = Field(type, nameof(RefreshToken.LastActivityAt)),
            ["revokedAt"] = Field(type, nameof(RefreshToken.RevokedAt)),
            ["ipAddress"] = Field(type, nameof(RefreshToken.IpAddress)),
            ["device"] = Field(type, nameof(RefreshToken.Device)),
            ["browser"] = Field(type, nameof(RefreshToken.Browser)),
            ["operatingSystem"] = Field(type, nameof(RefreshToken.OperatingSystem)),
            ["isSuspicious"] = Field(type, nameof(RefreshToken.IsSuspicious)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyEntityMetadata CreateErrorLogsMetadata()
    {
        var type = typeof(ErrorLog);
        var fields = new Dictionary<string, AccessPolicyFieldMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = Field(type, nameof(ErrorLog.Id)),
            ["timestamp"] = Field(type, nameof(ErrorLog.Timestamp)),
            ["exceptionType"] = Field(type, nameof(ErrorLog.ExceptionType)),
            ["statusCode"] = Field(type, nameof(ErrorLog.StatusCode)),
            ["userId"] = Field(type, nameof(ErrorLog.UserId)),
            ["userName"] = Field(type, nameof(ErrorLog.UserName)),
            ["path"] = Field(type, nameof(ErrorLog.Path)),
            ["method"] = Field(type, nameof(ErrorLog.Method)),
        };

        return new AccessPolicyEntityMetadata
        {
            EntityType = type,
            Fields = fields,
        };
    }

    private static AccessPolicyFieldMetadata Field(Type entityType, string propertyName)
    {
        var prop = entityType.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on {entityType.Name}.");

        return new AccessPolicyFieldMetadata
        {
            PropertyName = prop.Name,
            ClrType = prop.PropertyType,
        };
    }
}
