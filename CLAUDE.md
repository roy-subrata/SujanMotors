# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SujanMotors / AutoPartShop** — an auto parts shop management system with a .NET 10 REST API backend, Angular 20 frontend, and a Python AI sales agent.

## Development Commands

### Database (required first)
```bash
# Start SQL Server 2022 on port 1433
docker compose -f deployment/docker-compose.yml up -d
```

### Backend API (.NET 10)
```bash
# Run the API (auto-applies EF migrations on startup)
dotnet run --project src/AutoPartShop.Api

# Build the entire solution
dotnet build AutoPartShop.sln

# Add a new EF migration
dotnet ef migrations add <MigrationName> --project src/AutoPartShop.Infrastructure --startup-project src/AutoPartShop.Api

# Run tests (no test projects currently exist)
```

### Frontend (Angular 20)
```bash
cd src/AutoPartShop.WebApp
npm start          # dev server on :4200
npm run build      # production build
npm test           # Karma/Jasmine unit tests
npm run format     # Prettier formatter
```

### AI Agent (Python / Chainlit)
```bash
cd src/AutoPartShop.AI
uv sync            # install deps
chainlit run app/chainlit_app.py
```

## Architecture

### Backend — Clean Architecture

```
src/
  AutoPartShop.Domain/          # Entities, repository interfaces, value objects
  AutoPartShop.Application/     # DTOs, service interfaces, use-case services
  AutoPartShop.Infrastructure/  # EF Core DbContext, repository implementations, migrations
  AutoPartShop.Api/             # ASP.NET Core controllers, JWT middleware, Program.cs
```

**Dependency direction**: Api → Application → Domain; Infrastructure → Domain + Application.

DI is wired via `AddApplication()` and `AddInfrastructure()` extension methods in each layer's `Dependency.cs`.

All domain entities inherit from `AuditableEntity` (created/modified timestamps). The pattern throughout is: controller → application service (via interface) → domain repository (via interface, implemented in Infrastructure).

**Key infrastructure details**:
- `AutoPartDbContext` (EF Core / SQL Server) — entity configurations live in `Infrastructure/Data/Configurations/`
- `DatabaseSeeder.SeedAsync()` runs on startup to seed initial data
- Migrations are applied automatically on startup via `app.ApplyMigration()` (extension in Infrastructure)
- FluentResults (`Result<T>`) used in Infrastructure services for error propagation

**Auth**: ASP.NET Core Identity + JWT Bearer. Tokens configured in `appsettings.json` under `JwtSettings`. Controllers use `[Authorize]`; role-based with `ApplicationRole`.

**Observability**: Serilog structured logging → OpenTelemetry → OTLP collector. Prometheus scrape endpoint at `/metrics`. Swagger UI at `/docs`.

**API conventions**:
- Route: `[Route("api/[controller]")]`
- JSON: camelCase, string enums
- CORS: `AllowAnyOrigin` (all origins allowed — suitable for Cloudflare tunnels)

### Frontend — Angular 20 (Standalone Components)

```
src/AutoPartShop.WebApp/src/app/
  features/        # Feature modules: admin, audit, dashboard, ecommerce,
                   #   inventory, procurement, sales, warranty
  layout/          # Shell layout components (sidebar, navbar)
  pages/           # Top-level routed pages
  shared/          # components, constants, directives, guards,
                   #   interceptors, pipes, services
```

Each feature folder (e.g., `features/inventory/`) contains sub-features (brands, categories, parts, stock, etc.) each with their own component and service. Routing is lazy-loaded per feature via `<feature>.routes.ts`.

**UI stack**: PrimeNG v20 components + TailwindCSS v4 (PostCSS plugin, no config file).

**Key shared services**: `auth.service.ts`, `currency.service.ts`, `pricing-validation.service.ts`, `audit-trail.service.ts`.

The app is also a PWA (`@angular/service-worker`, `ngsw-config.json`).

### AI Agent (Python)

```
src/AutoPartShop.AI/app/
  agent.py          # LangGraph agent definition
  chainlit_app.py   # Chainlit UI entry point
  config.py         # Settings (env-based via pydantic-settings)
  models.py         # Pydantic models
  state.py          # LangGraph state schema
  services/         # API client, tool implementations
  prompts/          # System prompts
```

Uses LangGraph for multi-step agentic workflows, OpenAI as the LLM, Qdrant for customer memory (vector store), and Chainlit as the chat UI. Configured entirely via environment variables (see `.env.example`).

### Deployment

`deployment/docker-compose.yml` contains service definitions for:
- SQL Server (always-on, required for local dev)
- API, WebApp, AI agent (commented out — build locally instead)
- Full observability stack: OTel Collector, Prometheus, Loki, Tempo, Grafana (commented out)

## Important Notes

- All documentation `.md` files belong in the `docs/` folder, not the project root (this `CLAUDE.md` is the exception — Claude Code loads it from the root). Keep `docs/` to current reference documentation — no one-off fix summaries or work-completed reports.
- The solution file `AutoPartShop.sln` at the root only references the backend projects; the Angular project is managed separately via `npm`.
- `appsettings.Development.json` overrides the SQL Server connection string for local development.
- The `src/AutoPartShop.AI/.env` file (gitignored) must be configured from `.env.example` before running the AI agent.
