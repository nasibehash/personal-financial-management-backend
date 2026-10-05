using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Infrastructure.Options;
using PersonalFinancialManagement.Infrastructure.Persistence;
using PersonalFinancialManagement.Infrastructure.Services;
using PersonalFinancialManagement.Infrastructure.Services.Ai;

namespace PersonalFinancialManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(PostgresConnectionString.Normalize(connectionString)));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddHttpClient<ILlmClient, OpenAiChatClient>(ConfigureAiClient);
        services.AddHttpClient<ISpeechToTextService, OpenAiSpeechToTextService>(ConfigureAiClient);
        services.AddTransient<LlmTransactionTextParser>();
        services.AddSingleton<RuleBasedTransactionTextParser>();
        services.AddTransient<ITransactionTextParser, TransactionTextParser>();
        services.AddTransient<LlmFinancialInsightGenerator>();
        services.AddSingleton<RuleBasedInsightGenerator>();
        services.AddTransient<IFinancialInsightGenerator, FinancialInsightGenerator>();

        return services;
    }

    private static void ConfigureAiClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        if (options.IsConfigured)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
}
