# algo.bytes Backend

Backend API for the `algo.bytes` full-stack architecture template. The backend
uses a layered .NET architecture to support authentication, user management,
role management, access policies, audit logs, error logs, and system monitoring.

## Architecture

```text
src/
+-- algo.API             # HTTP API, controllers, filters, middleware, OpenAPI
+-- algo.Application     # Use cases, commands, queries, DTOs, validators
+-- algo.Domain          # Domain entities, enums, policies
+-- algo.Infrastructure  # JWT, email, current user, external integrations
+-- algo.Persistence     # EF Core context, migrations, entity configuration
+-- algo.SharedKernel    # Shared constants and cross-cutting primitives
```

The backend follows clean architecture principles:

- `algo.API` handles transport concerns only.
- `algo.Application` owns use cases and contracts.
- `algo.Domain` holds core entities and domain concepts.
- `algo.Infrastructure` implements external services.
- `algo.Persistence` implements database access and EF Core mappings.

## Features

- JWT login, refresh token, logout, registration, OTP verification, forgot
  password, and reset password
- ASP.NET Core Identity user storage
- User administration with create, update, delete, activate, deactivate, lock,
  unlock, email confirmation, password changes, and role assignment
- Role CRUD and role details
- RBAC permissions with default roles and permissions
- Access policy management for fine-grained authorization rules
- Access policy condition parsing, validation, and evaluation
- Application request logging and structured Serilog output
- Error logging and searchable log endpoints
- Pagination, filtering, and sorting helpers
- Database seeding for baseline identity data
- Unit, integration, and architecture test projects

## Tech Stack

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL with Npgsql
- ASP.NET Core Identity
- JWT bearer authentication
- MediatR
- FluentValidation
- Mapster
- Serilog
- Scalar API reference

## Prerequisites

- .NET 10 SDK
- PostgreSQL
- EF Core CLI tools, if creating or applying migrations manually

Install EF Core tools if needed:

```bash
dotnet tool install --global dotnet-ef
```

## Configuration

Main configuration files:

- `src/algo.API/appsettings.json`
- `src/algo.API/appsettings.Development.json`

Important sections:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=algo;Username=postgres;Password=root"
  },
  "Jwt": {
    "Issuer": "algo.bytes",
    "Audience": "algo.bytes",
    "SigningKey": "replace-with-secure-secret"
  },
  "Otp": {
    "ExpirationMinutes": 10,
    "CodeLength": 6,
    "Pepper": "replace-with-secure-secret"
  }
}
```

Use environment variables or a secret manager for production secrets.

## Run Locally

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project src/algo.API/algo.API.csproj
```

The API launch profiles expose:

- `https://localhost:7259`
- `http://localhost:5244`

## API Documentation

In development, OpenAPI and Scalar are enabled.

Open Scalar in the browser:

```text
https://localhost:7259/scalar
```

The API uses JWT bearer security. After login, paste the access token into the
Scalar bearer token authorization field.

## Database

The application uses EF Core migrations in:

```text
src/algo.Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project src/algo.Persistence/algo.Persistence.csproj --startup-project src/algo.API/algo.API.csproj
```

Create a new migration:

```bash
dotnet ef migrations add MigrationName --project src/algo.Persistence/algo.Persistence.csproj --startup-project src/algo.API/algo.API.csproj
```

The API seeds baseline data during startup through `ApplicationDbContextSeeder`.

## Tests

Run all backend tests from the repository root:

```bash
dotnet test
```

Test projects live under `tests/` and include application, domain, API,
persistence, and architecture test projects.

## Key Endpoints

The API currently includes controllers for:

- Authentication
- Users
- Roles
- Access policies
- Application logs
- Error logs

Use Scalar for the full current endpoint list and request/response contracts.
