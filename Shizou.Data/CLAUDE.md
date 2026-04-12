# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this project.

## Overview

Data layer with EF Core context, entity models, migrations, command args, and filter criteria. Uses SQLite.

## Database Contexts

- **ShizouContext**: Main database (ShizouDB.sqlite3). 24 DbSets via IShizouContext interface. Custom `ShizouDbSet` wrapper replaces standard EF DbSet finder.
  Database path is per-user: `ShizouDB[.username].sqlite3`.
- **AuthContext**: Separate IdentityDbContext (IdentityDB.sqlite3) for ASP.NET Identity isolation.

## Value Converters

- **CommandArgsConverter**: Polymorphic JSON for CommandArgs using `PolymorphicJsonTypeInfo<T>` (reflection-based subclass discovery with class name
  discriminator).
- **AnimeCriterionConverter**: Same polymorphic approach for filter criteria.
- **DateTimeConverter / NullableDateTimeConverter**: UTC normalization for all DateTime properties.

## Models

- **AniDb entities**: AniDbAnime, AniDbEpisode, AniDbFile (base) → AniDbNormalFile / AniDbGenericFile, AniDbGroup, AniDbCharacter, AniDbCreator, AniDbCredit.
- **Local**: LocalFile, ImportFolder, FileWatchedState.
- **Junction tables**: MalAniDbXref, AniDbEpisodeFileXref (many-to-many).
- **Command/Scheduling**: CommandRequest, ScheduledCommand, Timer.
- **JSON-owned entities**: AniDbNormalFile.Audio, Subtitles, Video stored as JSON blobs in OnModelCreating.

## CommandInputArgs

Abstract record `CommandArgs(CommandId, CommandPriority, QueueType)` with 18 concrete subclasses (e.g., AnimeArgs, HashArgs, ProcessArgs). Each generates a
unique CommandId string. Polymorphic JSON serialization via `PolymorphicJsonTypeInfo`.

## FilterCriteria

Expression-based anime filtering that translates to EF LINQ:

- `TermCriterion` (abstract): Single condition with `Negated` bool, wraps `PredicateInner` with Expression.Not.
- Concrete terms: AnimeTypeCriterion, TagCriterion, AirDateCriterion, SeasonCriterion, etc.
- **AndAllCriterion**: All terms match. **OrAnyCriterion**: Any AND group matches (stored as JSON in AnimeFilter).
- `ParameterReplacer` rebinds expression parameters when combining predicates.

## FilePaths

`FilePaths.cs` provides platform-aware paths (Windows %AppData%, Linux ~/.config, macOS ~/Library). Defines all content directories (images, logs, cache, certs,
etc.) and database file paths.

## Enums

`Enums/AniDb.cs` contains anime domain enums with XmlEnum mappings. `EpisodeType` has extension methods (`GetPrefix`, `GetEpString`, `ParseEpString`) for
episode string formatting (e.g., "S5" for Special 5).
