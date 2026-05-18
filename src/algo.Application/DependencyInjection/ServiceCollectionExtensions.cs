using System.Reflection;
using algo.Application.Behaviors;
using algo.Application.Common.CustomFields;
using algo.Application.Configuration;
using algo.Application.Features.Auth.Mapping;
using algo.Application.Features.ErrorLogs.Mapping;
using algo.Application.Features.Logs.Mapping;
using algo.Application.Features.Roles.Mapping;
using algo.Application.Features.Users.Mapping;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace algo.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<CustomFieldValueValidator>();

        new AuthMappingConfig().Register(TypeAdapterConfig.GlobalSettings);
        new UsersMappingConfig().Register(TypeAdapterConfig.GlobalSettings);
        new RolesMappingConfig().Register(TypeAdapterConfig.GlobalSettings);
        new LogsMappingConfig().Register(TypeAdapterConfig.GlobalSettings);
        new ErrorLogsMappingConfig().Register(TypeAdapterConfig.GlobalSettings);

        return services;
    }
}
