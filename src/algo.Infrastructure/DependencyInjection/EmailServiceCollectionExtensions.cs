using algo.Application.Abstractions;
using algo.Infrastructure.ExternalServices;
using Microsoft.Extensions.DependencyInjection;

namespace algo.Infrastructure.DependencyInjection;

public static class EmailServiceCollectionExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services)
    {
        services.AddScoped<IEmailConfirmationSender, EmailConfirmationSender>();
        services.AddScoped<IPasswordResetEmailSender, PasswordResetEmailSender>();
        return services;
    }
}
