using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinancialManagement.Application.Common.Behaviors;
using PersonalFinancialManagement.Application.Services;

namespace PersonalFinancialManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            // MediatR 13+ needs a license key for commercial use (free Community key available).
            // Set it in appsettings.Local.json, user-secrets or the MEDIATR_LICENSE_KEY env var.
            cfg.LicenseKey = configuration["MediatR:LicenseKey"];

            cfg.RegisterServicesFromAssembly(assembly);

            // Behaviors run in the order they are registered: logging wraps validation.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddScoped<TransactionTextProcessor>();

        return services;
    }
}
