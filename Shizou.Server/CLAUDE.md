# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this project.

## Overview

Core business logic library. Contains API controllers, AniDB integration, command processing, services, and native interop.

## Command Pattern

- `Command<T> : ICommand<T>` where `T : CommandArgs` — override `ProcessInnerAsync()` for work.
- 18 concrete command types (e.g., HashCommand, AnimeCommand, ProcessCommand) registered as Transient.
- `CommandProcessor` (abstract BackgroundService) manages priority-based sorted queues with pause/unpause, poll interval backoff, and INotifyPropertyChanged for
  UI binding.
- 5 processor subclasses: `AniDbUdpProcessor`, `AniDbHttpProcessor`, `HashProcessor`, `GeneralProcessor`, `ImageProcessor`.
- `CommandService` (Singleton HostedService) dispatches commands to queues and runs a 1-minute periodic timer for scheduled commands.

## Controllers

- `EntityGetController<TEntity>` — generic read-only base with `[HttpGet]` and `[HttpGet("{id:int}")]`.
- `EntityController<TEntity>` extends it with POST/PUT/DELETE. Uses expression-based ID selectors.
- All controllers require authorization; specific endpoints opt out with `[AllowAnonymous]`.

## AniDB API Integration

- **AniDbUdpState**: Manages UDP session lifecycle (login/logout), 12-hour ban tracking persisted via Timer model, NAT traversal (UPnP/PMP via Mono.Nat),
  auto-logout after 10 min inactivity.
- **AniDbHttpState**: Simpler ban-only state tracking.
- **Rate limiters** (`AniDbApi/RateLimiters/`): SemaphoreSlim + Stopwatch based, returning disposable wrappers. UDP: 2.5s short / 4.5s long delay; HTTP: 5s
  constant.
- Request types use generic factory pattern: `AddGenericFactory<IAuthRequest, AuthRequest>()` creates `Func<IAuthRequest>` singletons.

## RHash Native Interop

- `RHasher` (`RHash/RHasher.cs`): P/Invoke wrapper around librhash C library.
- `NativeLibrary.SetDllImportResolver()` for platform-specific loading (win-x64, linux-x64, linux-arm64 runtimes in `runtimes/`).
- `HashIds` flags enum enables combining multiple hash algorithms in a single pass.

## DI & Initialization

`Extensions/InitializationExtensions.cs` (~417 lines) is the central DI setup:

- `AddShizouServices()`: DbContextFactory, Identity, all commands and services.
- `AddShizouProcessors()`: 5 processor types, 30s shutdown timeout.
- `AddAniDbServices()`: API state singletons, rate limiters, generic request factories, named HttpClient with GZip.
- `AddShizouApiServices()`: Swagger, ProblemDetails, JSON-only formatters.
- `AddShizouOptions()`: Config loading, validation, SSL cert auto-detection, IP type converters.
- `UseSecurityHeaders()`: CSP with Blazor import map SHA256 hashing.

## Options

`ShizouOptions` — hierarchical config (Import, AniDb, MyAnimeList, CollectionView, ExternalLogin sub-options). Thread-safe file I/O with Lock. JSON Schema
generation via Json.Schema library.
