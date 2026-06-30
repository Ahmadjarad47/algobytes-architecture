using algo.Application.Abstractions;
using algo.Infrastructure.Identity;
using algo.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace algo.Infrastructure.DependencyInjection;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddSingleton<OtpHasher>();
        services.AddScoped<IOtpCodeGenerator, OtpCodeGenerator>();
        services.AddScoped<IOtpCodeVerifier, OtpCodeVerifier>();

        services.AddScoped<IRefreshTokenHasher, RefreshTokenHasherCore>();
        services.AddScoped<IAccessTokenFactory, AccessTokenFactory>();
        services.AddScoped<IRefreshTokenFactory, RefreshTokenFactory>();

        return services;
    }
}
