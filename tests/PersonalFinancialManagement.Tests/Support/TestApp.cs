using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinancialManagement.Application;
using PersonalFinancialManagement.Application.DTOs;
using PersonalFinancialManagement.Application.Features.Commands.CreateAccount;
using PersonalFinancialManagement.Application.Features.Commands.RegisterUser;
using PersonalFinancialManagement.Application.Features.Queries.GetCategories;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Domain.Enums;
using PersonalFinancialManagement.Infrastructure.Options;
using PersonalFinancialManagement.Infrastructure.Persistence;
using PersonalFinancialManagement.Infrastructure.Services;

namespace PersonalFinancialManagement.Tests.Support;

// Builds the real application pipeline (MediatR + validation + EF Core) on top of an
// in-memory SQLite database, with a controllable clock and "current user".
public sealed class TestApp : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestApp(Action<IServiceCollection>? configure = null)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "test-secret-key-for-unit-tests-0123456789"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(configuration);

        services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
        services.AddScoped<ApplicationDbContext>(sp =>
            new SqliteApplicationDbContext(sp.GetRequiredService<DbContextOptions<ApplicationDbContext>>()));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IDateTimeProvider>(Clock);
        services.AddSingleton<ICurrentUserService>(CurrentUser);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        configure?.Invoke(services);

        Services = services.BuildServiceProvider(validateScopes: true);

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public FakeClock Clock { get; } = new(new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc));

    public FakeCurrentUser CurrentUser { get; } = new();

    // Each call runs in its own scope, like a separate HTTP request.
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    public async Task Send(IRequest request)
    {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    // Direct database access for arranging data and asserting on stored state.
    public async Task<T> Query<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    public async Task Execute(Func<ApplicationDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await action(db);
        await db.SaveChangesAsync();
    }

    // Registers a user and makes it the current user.
    public async Task<UserDto> SignUp(string email = "sara@example.com", string fullName = "Sara Ahmadi")
    {
        var result = await Send(new RegisterUserCommand(fullName, email, "Passw0rd123"));
        CurrentUser.Id = result.User.Id;
        return result.User;
    }

    public Task<AccountDto> CreateAccount(string name = "Main wallet", decimal initialBalance = 0)
        => Send(new CreateAccountCommand(name, AccountType.Cash, initialBalance));

    // Looks up one of the current user's categories by name.
    public async Task<Guid> CategoryId(string name, CategoryType type)
    {
        var categories = await Send(new GetCategoriesQuery(type));
        return categories.Single(c => c.Name == name).Id;
    }

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }
}
