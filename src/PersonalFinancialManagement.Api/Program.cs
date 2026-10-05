using Microsoft.EntityFrameworkCore;
using PersonalFinancialManagement.Api.Middleware;
using PersonalFinancialManagement.Application;
using PersonalFinancialManagement.Infrastructure;
using PersonalFinancialManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Local-only overrides (e.g. MediatR:LicenseKey); this file is git-ignored.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply pending EF Core migrations automatically while developing.
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
