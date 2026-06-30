using algo.Application.Abstractions;
using algo.Infrastructure.ExternalServices;
using algo.Infrastructure.Identity;
using algo.Infrastructure.Logging;
using algo.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace algo.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));
        services.AddSingleton<LoggingEnricher>();

        services.AddHttpContextAccessor();

        services.AddIdentityServices();
        services.AddEmailServices();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ISessionContext, SessionContext>();
        services.AddScoped<IAccessPolicyTokenResolver, AccessPolicyTokenResolver>();

        return services;
    }
}
