using System.Data.Common;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Api.Extensions;
using PersonalFinancialManagement.Api.Middleware;
using PersonalFinancialManagement.Api.Services;
using PersonalFinancialManagement.Application;
using PersonalFinancialManagement.Application.Interfaces;
using PersonalFinancialManagement.Infrastructure;
using PersonalFinancialManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Local-only overrides (e.g. MediatR:LicenseKey); this file is git-ignored.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply pending EF Core migrations automatically while developing.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        await db.Database.MigrateAsync();
    }
    catch (DbException ex)
    {
        // Most often SQL Server (or LocalDB) is not installed or the connection string points to the wrong server.
        app.Logger.LogCritical(
            "Could not set up the database on '{DataSource}': {Message}\n" +
            "Check 'ConnectionStrings:DefaultConnection'. To use another SQL Server, put the connection string in " +
            "appsettings.Local.json (see the README, section \"Database connection problems\").",
            db.Database.GetDbConnection().DataSource,
            ex.Message);

        Environment.ExitCode = 1;
        return;
    }
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
