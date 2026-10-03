using PersonalFinancialManagement.Api.Middleware;
using PersonalFinancialManagement.Application;
using PersonalFinancialManagement.Infrastructure;

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
