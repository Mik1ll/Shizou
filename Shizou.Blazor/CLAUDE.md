# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this project.

## Overview

Blazor Server frontend and main application entry point. Hosts both the UI (interactive server rendering) and the API controllers from Shizou.Server.

## Key Concepts

- **Single-instance enforcement**: Program.cs uses a named Mutex with 2-second timeout to prevent multiple instances per user.
- **Rendering**: Interactive Server render mode. Account pages use static SSR (not interactive).
- **Authentication**: Cookie-based ASP.NET Identity with `IdentityRevalidatingAuthenticationStateProvider` (revalidates security stamp every 30 min). Separate
  auth database (IdentityDB.sqlite3). All routes require authorization by default via `_Imports.razor`.
- **Database**: Auto-migrates on startup.

## Component Conventions

- Pages live in `Components/Pages/`, with sub-feature folders (e.g., `Pages/Anime/Components/`).
- `[EditorRequired]` on required `[Parameter]` properties.
- `[SupplyParameterFromForm]` for EditForm binding with `DataAnnotationsValidator`.
- Component-scoped CSS via `.razor.css` files.
- `Blazored.Modal` for modal dialogs, `ToastService` (scoped) for notifications.

## Frontend Assets

- **libman.json**: Bootstrap 5.3.8, Bootstrap Icons, video.js from jsdelivr CDN.
- **package.json**: esbuild bundles jassub (subtitle renderer) to `wwwroot/js/jassub_dist`.
- **Theme**: `js/theme.js` manages light/dark/auto via localStorage and `data-bs-theme` attribute.
- **Blazor module** (`Shizou.Blazor.lib.module.js`): Lazy-imports video.js in `beforeWebStart`, resets theme on enhanced navigation.

Run `npm run build-jassub` after changing jassub dependencies.

## Middleware Stack

Security headers (CSP, X-Frame-Options) configured in `Shizou.Server/Extensions/InitializationExtensions.cs` with special handling for Blazor import map SHA256
hashes.
