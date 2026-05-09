using algo.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace algo.RealTime;

public static class RealTimeDependencyInjection
{
    public static IServiceCollection AddRealTime(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' was not found under ConnectionStrings.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSignalR();
        services.AddSingleton<UserPresenceTracker>();
        services.AddSingleton<ISessionRealtimeNotifier, SignalRSessionRealtimeNotifier>();
        return services;
    }
}
