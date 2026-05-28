using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace algo.API.HealthChecks;

public sealed class RedisHealthCheck(IConnectionMultiplexer multiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!multiplexer.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis connection is unavailable.");
        }

        try
        {
            var latency = await multiplexer.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy(
                "Redis connection is available.",
                new Dictionary<string, object>
                {
                    ["latencyMs"] = latency.TotalMilliseconds
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
        }
    }
}
