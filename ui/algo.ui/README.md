# algo.bytes Frontend

Angular frontend for the `algo.bytes` full-stack architecture template. The app
provides an admin dashboard experience for authentication, users, roles, access
policies, logs, reports, settings, and monitoring workflows.

## Features

- Login and registration screens
- Admin-configurable login and registration visuals through the Settings auth
  page designer
- Registration forms can render visible user custom fields from the backend
- Auth and guest route guards
- JWT-aware `/api/v1` API access through HTTP interceptors
- Dashboard layout with protected feature areas
- User management pages and API services
- Role management pages and API services
- Access policy management with condition builder support
- Application logs and error logs screens
- Settings and reports feature areas
- Shared admin table, form dialog, details drawer, and confirm dialog
- Theme service and toast service

## Tech Stack

- Angular 21
- Angular SSR
- PrimeNG
- PrimeIcons
- Tailwind CSS
- Chart.js
- RxJS
- Vitest

## Project Structure

```text
src/app/
+-- core/                  # API wrapper, guards, interceptors, config, models
+-- features/              # Feature modules and pages
|   +-- access-policies
|   +-- auth
|   +-- dashboard
|   +-- error-logs
|   +-- logs
|   +-- reports
|   +-- roles
|   +-- settings
|   +-- users
+-- layouts/               # Auth and dashboard layouts
+-- shared/                # Reusable admin components, models, utilities
```

## Prerequisites

- Node.js
- npm
- Running `algo.bytes` backend API

The project currently uses:

```text
npm@11.13.0
```

## Install

From this directory:

```bash
npm install
```

## Run Locally

Start the Angular dev server:

```bash
npm start
```

Open:

```text
http://localhost:4200
```

The app expects the backend API at:

```text
https://localhost:7259/api/v1
```

## API Configuration

The API base URL is configured in:

```text
src/environments/environment.ts
src/environments/environment.prod.ts
```

Default value:

```ts
apiBaseUrl: 'https://localhost:7259/api/v1'
```

Update this value when pointing the frontend to a different backend host. The
dashboard Settings page also stores a local API base URL override for admin
testing.

## Auth Page Designer

Admins can customize the login and create-account screens from:

```text
Dashboard -> Settings -> Auth page designer
```

The designer controls background colors, accent color, accent size and opacity,
card background, card border, card radius, card shadow, login/register card
widths, and button colors. The same settings drive the live preview,
`/auth/login`, and `/auth/register`.

## Available Scripts

```bash
npm start
```

Runs the development server.

```bash
npm run build
```

Builds the application.

```bash
npm run watch
```

Builds in watch mode with development configuration.

```bash
npm test
```

Runs unit tests with Vitest through Angular's test builder.

```bash
npm run serve:ssr:algo.ui
```

Serves the built SSR output from `dist/algo.ui/server/server.mjs`.

## Build

Create a production build:

```bash
npm run build
```

Build output is written to:

```text
dist/
```

## Development Notes

- Protected routes use `authGuard`.
- Authentication pages use `guestGuard`.
- API calls should go through services in each feature's `api/` folder.
- Shared admin UI patterns live in `src/app/shared/components`.
- Cross-cutting app services live in `src/app/core`.
- Keep frontend request/response interfaces in feature `models/` folders and
  avoid declaring ad-hoc API contracts directly inside components.
