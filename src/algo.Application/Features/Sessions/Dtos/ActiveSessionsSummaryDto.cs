namespace algo.Application.Features.Sessions.Dtos;

public sealed record ActiveSessionsSummaryDto(
    int OnlineUsers,
    int IdleUsers,
    int ActiveSessions,
    int SuspiciousSessions,
    int RevokedToday);
