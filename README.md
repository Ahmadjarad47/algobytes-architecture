# algo.bytes

Advanced full-stack architecture template for scalable backend and frontend
applications with user management, authentication, RBAC permissions, role
management, audit logs, and system monitoring.

## Overview

`algo.bytes` is a modular starter platform for building secure admin and
operations applications. It combines a .NET backend with an Angular frontend and
keeps the core business rules, persistence, infrastructure, and UI concerns in
separate layers.

The template is designed for teams that need a strong foundation for:

- User registration, login, OTP verification, refresh tokens, and password reset
- Role-based access control and role management
- Attribute-based access policies for fine-grained authorization
- User administration, locking, unlocking, activation, and role assignment
- Audit-style application logs and error logs
- Dashboard, reporting, settings, and monitoring screens
- Clean backend layering with test projects ready for expansion

## Repository Structure

```text
algo.bytes/
+-- src/                         # Backend source code
|   +-- algo.API                 # ASP.NET Core API, controllers, middleware
|   +-- algo.Application         # CQRS features, DTOs, validation, contracts
|   +-- algo.Domain              # Domain entities, policies, enums
|   +-- algo.Infrastructure      # JWT, email, current user, external services
|   +-- algo.Persistence         # EF Core DbContext, migrations, repositories
|   +-- algo.SharedKernel        # Shared constants and cross-cutting code
+-- tests/                       # Unit, architecture, and integration tests
+-- ui/algo.ui/                  # Angular frontend application
+-- algo.bytes.slnx              # .NET solution
+-- README.md
```

## Tech Stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL via Npgsql
- ASP.NET Core Identity
- JWT bearer authentication
- MediatR, FluentValidation, and Mapster
- Serilog structured logging
- Scalar API reference

### Frontend

- Angular 21
- Angular SSR support
- PrimeNG and PrimeIcons
- Tailwind CSS
- Chart.js
- RxJS
- Vitest

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js and npm
- PostgreSQL
- Git

### Backend

See [src/README.md](src/README.md) for backend setup, configuration, database,
migrations, API documentation details, and API contract structure conventions.

Quick start:

```bash
dotnet restore
dotnet build
dotnet run --project src/algo.API/algo.API.csproj
```

The API runs on:

- `https://localhost:7259`
- `http://localhost:5244`

Scalar API documentation is available in development at:

```text
https://localhost:7259/scalar
```

### Frontend

See [ui/algo.ui/README.md](ui/algo.ui/README.md) for frontend setup,
configuration, scripts, and feature structure.

Quick start:

```bash
cd ui/algo.ui
npm install
npm start
```

The Angular app runs on:

```text
http://localhost:4200
```

## Configuration

Backend configuration lives in:

- `src/algo.API/appsettings.json`
- `src/algo.API/appsettings.Development.json`

Frontend API configuration lives in:

- `ui/algo.ui/src/app/core/config/app-config.token.ts`

For production, move secrets such as database passwords, JWT signing keys, and
OTP peppers into environment variables or a secure secret store.

## Development Workflow

1. Start PostgreSQL.
2. Run the backend API.
3. Run the Angular frontend.
4. Open the frontend at `http://localhost:4200`.
5. Use Scalar at `https://localhost:7259/scalar` to inspect and test API
   endpoints.

## Testing

Backend tests:

```bash
dotnet test
```

Frontend tests:

```bash
cd ui/algo.ui
npm test
```

## License

Add your project license here.
