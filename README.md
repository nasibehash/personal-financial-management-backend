# Personal Financial Management

A layered ASP.NET Core Web API (.NET 10) that helps people record, manage and understand their own
finances, with AI-assisted entry (text and voice) and short insights about their spending.

## Features

| Module | What it does |
|---|---|
| **Accounts** | Wallets and bank accounts with an initial balance; the current balance is derived from income, expenses and transfers. Accounts can be archived. |
| **Transactions** | Income, expenses and transfers between accounts; filter by date, type, account, category and text; paged. |
| **Categories** | Income/expense categories per user. A default set is created at registration. |
| **Reports** | Summary (income, expense, net, savings rate, balance), spending per category, monthly trend and comparison with the previous period. |
| **Goals** | Savings goals with a target, optional deadline and contributions. Progress, remaining amount and the monthly saving needed are computed. |
| **AI** | Record a transaction from a sentence or a voice recording, and get insights about income, spending and goals. |
| **Auth** | Registration and login with JWT; every user only sees their own data. |

## Solution layout

| Project | Role |
|---|---|
| `PersonalFinancialManagement.Domain` | Entities and enums |
| `PersonalFinancialManagement.Application` | Use cases as MediatR requests and handlers, validators, pipeline behaviors, abstractions |
| `PersonalFinancialManagement.Infrastructure` | EF Core (SQL Server), JWT and password hashing, AI/speech clients |
| `PersonalFinancialManagement.Api` | Controllers, authentication, error handling |
| `PersonalFinancialManagement.Tests` | xUnit tests (handlers run through the real MediatR pipeline on in-memory SQLite) |

Dependencies point inwards: `Api → Infrastructure → Application → Domain`. Handlers talk to the database
through `IApplicationDbContext`, there are no repositories.

## Getting started

Requirements: the .NET 10 SDK and a SQL Server instance (LocalDB is enough).

```bash
dotnet restore
dotnet run --project src/PersonalFinancialManagement.Api
```

In the Development environment pending EF Core migrations are applied automatically, so the database
(`PersonalFinancialManagement`) is created on the first start. Sample requests for every endpoint are in
`src/PersonalFinancialManagement.Api/PersonalFinancialManagement.Api.http` (register first; the token is stored for the following requests).

### Configuration

Defaults live in `appsettings.json`. Put local or secret values in `appsettings.Local.json` (git-ignored, loaded
automatically) or in environment variables such as `Jwt__SecretKey`.

| Setting | Notes |
|---|---|
| `ConnectionStrings:DefaultConnection` | Defaults to SQL Server LocalDB. Other examples: `Server=.\\SQLEXPRESS;Database=PersonalFinancialManagement;Trusted_Connection=True;TrustServerCertificate=True` or `Server=localhost,1433;Database=PersonalFinancialManagement;User Id=sa;Password=<password>;TrustServerCertificate=True` |
| `Jwt:SecretKey` | **Required**, at least 32 characters. `appsettings.Development.json` contains a development-only key; set your own everywhere else. |
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` | Token settings (defaults are fine). |
| `Ai:ApiKey` | Optional, see [AI](#ai). |
| `MediatR:LicenseKey` | MediatR 13+ is commercially licensed (a free Community license exists). Without a key it still works and logs a license warning. |

### Database migrations

The initial migration is included (`Infrastructure/Persistence/Migrations`). After changing entities:

```bash
dotnet tool install --global dotnet-ef   # once
dotnet ef migrations add <Name> --project src/PersonalFinancialManagement.Infrastructure --startup-project src/PersonalFinancialManagement.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/PersonalFinancialManagement.Infrastructure --startup-project src/PersonalFinancialManagement.Api
```

### Tests

```bash
dotnet test
```

## API overview

All endpoints except `api/health`, `api/auth/register` and `api/auth/login` need an `Authorization: Bearer <token>` header.
Enums are sent and returned as strings (`"Expense"`), amounts are in the user's own (single) currency and dates are UTC.
Errors are returned as `application/problem+json` (`400` validation with per-field `errors`, `401`, `404`, `409`, `503`).

| Area | Endpoints |
|---|---|
| Auth | `POST api/auth/register`, `POST api/auth/login`, `GET api/auth/me` |
| Categories | `GET api/categories?type=`, `POST api/categories`, `PUT api/categories/{id}`, `DELETE api/categories/{id}` |
| Accounts | `GET api/accounts?includeArchived=`, `GET api/accounts/{id}`, `POST api/accounts`, `PUT api/accounts/{id}`, `DELETE api/accounts/{id}` |
| Transactions | `GET api/transactions?from=&to=&type=&accountId=&categoryId=&search=&page=&pageSize=`, `GET api/transactions/{id}`, `POST api/transactions`, `PUT api/transactions/{id}`, `DELETE api/transactions/{id}` |
| Reports | `GET api/reports/summary`, `GET api/reports/category-breakdown?type=`, `GET api/reports/monthly-trend?months=`, `GET api/reports/comparison` (all take optional `from` and `to`; default is the current month so far) |
| Goals | `GET api/goals?status=`, `GET api/goals/{id}`, `POST api/goals`, `PUT api/goals/{id}`, `DELETE api/goals/{id}`, `POST api/goals/{id}/contributions`, `DELETE api/goals/{id}/contributions/{contributionId}` |
| AI | `POST api/ai/transactions/text`, `POST api/ai/transactions/voice`, `GET api/ai/insights` |

Transfers between a user's own accounts are not counted as income or expense in reports.

## AI

Text entry, voice entry and insights use any **OpenAI-compatible** provider (chat completions and audio transcriptions).
Configure it in `appsettings.Local.json`:

```json
{
  "Ai": {
    "ApiKey": "<your key>",
    "BaseUrl": "https://api.openai.com/v1",
    "ChatModel": "gpt-4o-mini",
    "TranscriptionModel": "whisper-1",
    "Language": "fa"
  }
}
```

- **Text** (`POST api/ai/transactions/text`): `{ "text": "دیروز ناهار ۲۵۰ هزار تومان", "preview": false, "accountId": null }`.
  The sentence is turned into a transaction using the user's own categories and accounts. With `"preview": true` nothing is saved and the interpreted
  draft is returned so the client can ask for confirmation. Without `accountId` the account named in the text, or the user's only account, is used.
- **Voice** (`POST api/ai/transactions/voice`, `multipart/form-data`): the recording goes in the `audio` field (mp3, m4a, wav, webm, ogg, flac, up to 10 MB),
  with the optional `preview` and `accountId` fields. The audio is transcribed and then handled like text; the response includes the transcript.
- **Insights** (`GET api/ai/insights`): short observations about the period, based on income, spending, the previous period and active goals.

Without an `Ai:ApiKey`, or when the provider fails, text entry and insights fall back to built-in rules (Persian and English keywords for amounts,
categories and relative days such as «دیروز»), and the response says which method was used (`draft.method`, `generatedByAi`).
Voice entry needs the provider and answers `503` when it is not configured.

## Conventions

- One folder per use case under `Application/Features/Commands/<Name>` or `Application/Features/Queries/<Name>`, holding the request, its handler and an optional validator.
  The namespace follows the folder, e.g. `PersonalFinancialManagement.Application.Features.Queries.GetHealth`.
- Pipeline behaviors (`Application/Common/Behaviors`): `LoggingBehavior` → `ValidationBehavior` → handler.
- Validation uses FluentValidation; validators are registered automatically. Business-rule failures use the exceptions in `Application/Common/Exceptions`,
  which the exception middleware maps to HTTP status codes.

## NuGet packages

| Project | Package | Version |
|---|---|---|
| Application | `MediatR` | 14.2.0 |
| Application | `FluentValidation.DependencyInjectionExtensions` | 12.1.1 |
| Application | `Microsoft.EntityFrameworkCore` | 10.0.12 |
| Application | `Microsoft.Extensions.Configuration.Abstractions` | 10.0.0 |
| Infrastructure | `Microsoft.EntityFrameworkCore.SqlServer` | 10.0.12 |
| Infrastructure | `Microsoft.Extensions.Http` | 10.0.12 |
| Infrastructure | `Microsoft.Extensions.Options.ConfigurationExtensions` | 10.0.12 |
| Infrastructure | `System.IdentityModel.Tokens.Jwt` | 8.14.0 |
| Api | `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.12 |
| Api | `Microsoft.AspNetCore.OpenApi` | 10.0.12 |
| Api | `Microsoft.EntityFrameworkCore.Design` | 10.0.12 |
| Tests | `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` | 10.0.12, 18.10.1, 2.9.3, 3.1.5 |
