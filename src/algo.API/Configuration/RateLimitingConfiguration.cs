using algo.API.Security;
using System.Globalization;
using System.Threading.RateLimiting;

namespace algo.API.Configuration;

internal static class RateLimitingConfiguration
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfterSeconds = TryGetRetryAfterSeconds(context.Lease);
                if (retryAfterSeconds is not null)
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        retryAfterSeconds.Value.ToString(CultureInfo.InvariantCulture);
                }

                var problem = ProblemDetailsResponse.Create(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "Too many requests.",
                    "The request rate limit was exceeded. Try again later.");

                if (retryAfterSeconds is not null)
                {
                    problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
                }

                await ProblemDetailsResponse.WriteAsync(context.HttpContext, problem, cancellationToken);
            };

            options.AddPolicy(RateLimitPolicyNames.AuthLogin, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildClientPartitionKey(httpContext, RateLimitPolicyNames.AuthLogin),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicyNames.AuthOtp, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildClientPartitionKey(httpContext, RateLimitPolicyNames.AuthOtp),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicyNames.AuthPasswordReset, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildClientPartitionKey(httpContext, RateLimitPolicyNames.AuthPasswordReset),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(RateLimitPolicyNames.AuthRefreshToken, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    BuildClientPartitionKey(httpContext, RateLimitPolicyNames.AuthRefreshToken),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    private static string BuildClientPartitionKey(HttpContext context, string policyName)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var clientIp = forwardedFor?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return $"{policyName}:{clientIp}";
    }

    private static int? TryGetRetryAfterSeconds(RateLimitLease lease)
    {
        if (!lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
    }
}
