namespace algo.RealTime;

internal static class PresenceGroupNames
{
    public static string Session(Guid sessionId) => $"session:{sessionId:D}";
    public static string User(string userId) => $"user:{userId}";
}
