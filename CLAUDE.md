# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Shizou is an anime collection manager and file server that uses AniDB and MyAnimeList APIs for metadata and user lists. It's a .NET 10 full-stack application
with a Blazor Server frontend and ASP.NET Core backend using SQLite via Entity Framework Core.

## Common Commands

```bash
# Build
dotnet build Shizou.slnx

# Run (main app - Blazor UI + API on ports 8080/8443)
dotnet run --project Shizou.Blazor/Shizou.Blazor.csproj

# Run tests
dotnet test Shizou.Tests/Shizou.Tests.csproj

# Run a single test
dotnet test Shizou.Tests/Shizou.Tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Generate OpenAPI spec (auto-runs on Debug build of WebApi)
dotnet build Shizou.WebApi/Shizou.WebApi.csproj

# Docker build (multi-arch: amd64/arm64)
docker build -t shizou .

# Frontend assets (jassub subtitle worker)
npm run build-jassub
```

## Architecture

**Solution projects:**

- **Shizou.Blazor** — Main entry point. Blazor Server UI with Bootstrap 5, video.js, jassub for subtitle rendering. Cookie-based Identity authentication.
- **Shizou.Server** — Core business logic: AniDB UDP/HTTP API integration, command pattern for async operations, REST controllers, services, background
  processors. Uses `AllowUnsafeBlocks` for native RHash interop.
- **Shizou.Data** — EF Core context (`ShizouContext`), entity models, migrations, enums, value converters. SQLite database with UTC DateTime normalization and
  JSON-serialized owned types.
- **Shizou.WebApi** — Standalone API host (alternative to Blazor host).
- **Shizou.HttpClient** — NSwag auto-generated HTTP client, published as NuGet package.
- **Shizou.HealthChecker** — Docker health check utility.
- **Shizou.Tests** — MSTest with Moq, parallel execution enabled.

**Key patterns:**

- **Command pattern**: `Command` base class in Shizou.Server/Commands/ for queue-able async operations, with `CommandArgs` stored as polymorphic JSON in the
  database.
- **Generic entity controllers**: `EntityController<TEntity>` and `EntityGetController<TEntity>` provide reusable CRUD endpoints.
- **AniDB rate limiting**: UDP and HTTP API clients implement protocol-specific rate limiting and connection management.

## Build & Code Style

- Code style is enforced at build time (`EnforceCodeStyleInBuild`). Nullable reference types and implicit usings are enabled globally.
- `Microsoft.VisualStudio.Threading.Analyzers` is used across all projects.
- Target framework is .NET 10.0 with `global.json` pinning SDK 10.0.0 (rollForward: latestMinor).
