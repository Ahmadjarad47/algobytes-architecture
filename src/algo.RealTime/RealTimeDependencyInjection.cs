using algo.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace algo.RealTime;

public static class RealTimeDependencyInjection
{
    public static IServiceCollection AddRealTime(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<UserPresenceTracker>();
        services.AddSingleton<ISessionRealtimeNotifier, SignalRSessionRealtimeNotifier>();
        return services;
    }
}
