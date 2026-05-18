using System.Text.Json;
using algo.Domain.CustomFields;

namespace algo.Domain.Identity.Policies;

public sealed class AccessPolicy : IHasCustomFields
{
    public Guid Id { get; set; }

    public string Resource { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public AccessPolicyEffect Effect { get; set; }

    public AccessPolicySubjectType SubjectType { get; set; }

    public string SubjectKey { get; set; } = string.Empty;

    public string? ConditionJson { get; set; }

    public int Priority { get; set; }

    public bool IsEnabled { get; set; } = true;

    public string? Description { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public DateTime? TrashedAt { get; set; }

    public DateTime? TrashExpiresAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public string? UpdatedByUserId { get; set; }

    public JsonDocument? CustomFields { get; set; }
}
