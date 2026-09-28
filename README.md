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

## NuGet packages

| Project | Package | Version |
|---|---|---|
| Application | `MediatR` | 14.2.0 |
| Application | `FluentValidation.DependencyInjectionExtensions` | 12.1.1 |
| Application | `Microsoft.Extensions.Configuration.Abstractions` | 10.0.0 |
| Api | `Microsoft.AspNetCore.OpenApi` | 10.0.12 |

### License key

MediatR 13+ is commercially licensed (a free Community license is available). Provide the key via
`MediatR:LicenseKey` in the git-ignored `appsettings.Local.json`, user-secrets, or the `MEDIATR_LICENSE_KEY`
environment variable. Without a key MediatR still works but logs a license warning.

## Run

```bash
dotnet run --project src/PersonalFinancialManagement.Api
```
