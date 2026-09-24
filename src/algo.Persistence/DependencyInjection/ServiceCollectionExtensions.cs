using algo.Application.Abstractions;
using algo.Domain.Identity.Entities;
using algo.Persistence.Abac;
using algo.Persistence.Context;
using algo.Persistence.Interceptors;
using algo.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace algo.Persistence.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException(
                "Connection string 'Database' was not found under ConnectionStrings.");

        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>(
            (serviceProvider, options) =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());

                var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    options.EnableDetailedErrors();
                    options.EnableSensitiveDataLogging();
                }
            });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICustomFieldIndexSynchronizer, CustomFieldIndexSynchronizer>();
        services.AddScoped<ICustomFieldIndexDropper, CustomFieldIndexDropper>();
        services.AddHostedService<TrashFinalizationService>();

        services.AddSingleton<AccessPolicyMetadataProvider>();
        services.AddSingleton<IAccessPolicyMetadataLookup>(sp => sp.GetRequiredService<AccessPolicyMetadataProvider>());
        services.AddSingleton<IAccessPolicyResourceCatalog>(sp => sp.GetRequiredService<AccessPolicyMetadataProvider>());
        services.AddScoped<IAccessPolicyRuleStore, AccessPolicyRuleStore>();
        services.AddScoped<IAccessPolicyConditionParser, AccessPolicyConditionParser>();
        services.AddScoped<AccessPolicyExpressionCompiler>();
        services.AddScoped<IAccessPolicyAuthorizationChecker, AccessPolicyAuthorizationChecker>();
        services.AddScoped<IAccessPolicyQueryFilter, AccessPolicyQueryFilter>();

        return services;
    }
}
