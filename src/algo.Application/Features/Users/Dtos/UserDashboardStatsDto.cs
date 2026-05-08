namespace algo.Application.Features.Users.Dtos;

public sealed record UserDashboardStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int LockedUsers,
    int EmailConfirmedUsers,
    int EmailNotConfirmedUsers,
    int PhoneConfirmedUsers,
    int NewUsersToday,
    int NewUsersThisWeek,
    int NewUsersThisMonth,
    IReadOnlyDictionary<string, int> UsersByRole,
    IReadOnlyList<UserActivityDto> RecentUsers,
    IReadOnlyList<UserActivityDto> RecentlyLockedUsers);
