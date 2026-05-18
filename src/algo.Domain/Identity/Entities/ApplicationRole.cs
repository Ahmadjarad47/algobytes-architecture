using System.Text.Json;
using algo.Domain.CustomFields;
using Microsoft.AspNetCore.Identity;

namespace algo.Domain.Identity.Entities;

public sealed class ApplicationRole : IdentityRole, IHasCustomFields
{
    public DateTimeOffset? TrashedAt { get; set; }

    public DateTimeOffset? TrashExpiresAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public JsonDocument? CustomFields { get; set; }
}
