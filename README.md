# Personal Financial Management

A layered ASP.NET Core Web API (.NET 10) for managing personal finances.

## Projects

| Project | Role |
|---|---|
| `PersonalFinancialManagement.Domain` | Entities and domain rules |
| `PersonalFinancialManagement.Application` | Use cases as MediatR requests/handlers, validators, pipeline behaviors |
| `PersonalFinancialManagement.Infrastructure` | Persistence and external services |
| `PersonalFinancialManagement.Api` | HTTP endpoints; controllers send requests through MediatR |

## MediatR

- Registration: `Application/DependencyInjection.cs` (`services.AddApplication(configuration)`).
- Pipeline behaviors (`Application/Common/Behaviors`): `LoggingBehavior` → `ValidationBehavior` → handler.
- Validation uses FluentValidation. Validators in the Application assembly are registered automatically;
  failures return `400` with `ValidationProblemDetails` (see `Api/Middleware/ExceptionHandlingMiddleware.cs`).
- Add new use cases as request + handler + optional validator, one folder per use case:
  - Queries: `Application/Features/Queries/<Name>/` → namespace `PersonalFinancialManagement.Application.Features.Queries.<Name>`
  - Commands: `Application/Features/Commands/<Name>/` → namespace `PersonalFinancialManagement.Application.Features.Commands.<Name>`

  `Features/Queries/GetHealth/` is a minimal example.

### License key

MediatR 13+ is commercially licensed (a free Community license is available). Provide the key via
`MediatR:LicenseKey` in the git-ignored `appsettings.Local.json`, user-secrets, or the `MEDIATR_LICENSE_KEY`
environment variable. Without a key MediatR still works but logs a license warning.

## NuGet packages

| Project | Package | Version |
|---|---|---|
| Application | `MediatR` | 14.2.0 |
| Application | `FluentValidation.DependencyInjectionExtensions` | 12.1.1 |
| Application | `Microsoft.Extensions.Configuration.Abstractions` | 10.0.0 |
| Api | `Microsoft.AspNetCore.OpenApi` | 10.0.12 |
| Api | `Microsoft.EntityFrameworkCore.Design` | 10.0.12 |
| Infrastructure | `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.12 |

## Database (EF Core + SQL Server)

- `Infrastructure/Persistence/ApplicationDbContext.cs` implements `Application/Interfaces/IApplicationDbContext`.
- Entities inherit `Domain/Common/BaseEntity` (`Id`, `CreatedAtUtc`, `UpdatedAtUtc`); timestamps are set in `SaveChangesAsync`.
- Put `IEntityTypeConfiguration<T>` classes in `Infrastructure/Persistence/Configurations/`; they are applied automatically.
- Connection string: `ConnectionStrings:DefaultConnection` in `appsettings.json`. The default targets SQL Server LocalDB
  (installed with Visual Studio). For another server, override it in the git-ignored `appsettings.Local.json`, e.g.
  - SQL Server Express: `Server=.\\SQLEXPRESS;Database=PersonalFinancialManagement;Trusted_Connection=True;TrustServerCertificate=True`
  - SQL login / Docker: `Server=localhost,1433;Database=PersonalFinancialManagement;User Id=sa;Password=<password>;TrustServerCertificate=True`

Migrations (install the tool once with `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add InitialCreate --project src/PersonalFinancialManagement.Infrastructure --startup-project src/PersonalFinancialManagement.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/PersonalFinancialManagement.Infrastructure --startup-project src/PersonalFinancialManagement.Api
```

## Run

```bash
dotnet run --project src/PersonalFinancialManagement.Api
```
