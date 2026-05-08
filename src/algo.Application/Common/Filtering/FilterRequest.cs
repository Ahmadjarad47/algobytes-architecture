namespace algo.Application.Common.Filtering;

public sealed record FilterRequest(
    bool? IsActive = null,
    bool? IsLocked = null,
    bool? EmailConfirmed = null,
    bool? PhoneNumberConfirmed = null,
    string? RoleName = null,
    DateRangeFilter? CreatedAt = null,
    DateRangeFilter? LastLoginAt = null);
