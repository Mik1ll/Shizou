using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;
using Shizou.Data.Utilities;
using Timer = Shizou.Data.Models.Timer;

namespace Shizou.Data.Database;

public sealed class ShizouContext : DbContext, IShizouContext
{
    public ShizouContext(DbContextOptions<ShizouContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AniDbAnime>()
            .HasMany(a => a.MalAnimes)
            .WithMany(a => a.AniDbAnimes)
            .UsingEntity<MalAniDbXref>();
        modelBuilder.Entity<AniDbEpisode>()
            .HasMany(ep => ep.AniDbFiles)
            .WithMany(f => f.AniDbEpisodes)
            .UsingEntity<AniDbEpisodeFileXref>();
        modelBuilder.Entity<AniDbNormalFile>()
            .OwnsMany(f => f.Audio, builder => builder.ToJson())
            .OwnsMany(f => f.Subtitles, builder => builder.ToJson())
            .OwnsOne(f => f.Video, builder => builder.ToJson());
        modelBuilder.Entity<AnimeFilter>()
            .Property(f => f.Criteria)
            .HasConversion<AnimeCriterionConverter>();
        modelBuilder.Entity<CommandRequest>()
            .Property(cr => cr.CommandArgs)
            .HasConversion<CommandArgsConverter>();
        modelBuilder.Entity<ScheduledCommand>()
            .Property(cr => cr.CommandArgs)
            .HasConversion<CommandArgsConverter>();

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IDbSetFinder, ShizouDbSetFinder>();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveConversion<DateTimeConverter>();
        configurationBuilder
            .Properties<DateTime?>()
            .HaveConversion<NullableDateTimeConverter>();

        base.ConfigureConventions(configurationBuilder);
    }

    private class CommandArgsConverter : ValueConverter<CommandArgs, string>
    {
        public CommandArgsConverter() : base(
            ca => JsonSerializer.Serialize(ca, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo(null)),
            ca => JsonSerializer.Deserialize(ca, PolymorphicJsonTypeInfo<CommandArgs>.CreateJsonTypeInfo(null))!)
        {
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class DateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public DateTimeConverter() : base(v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class NullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableDateTimeConverter() : base(
            v => v.HasValue ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        {
        }
    }

    private class AnimeCriterionConverter : ValueConverter<OrAnyCriterion, string>
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            TypeInfoResolver = new PolymorphicJsonTypeResolver<TermCriterion>(),
        };

        public AnimeCriterionConverter() : base(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions),
            v => JsonSerializer.Deserialize<OrAnyCriterion>(v, JsonSerializerOptions)!
        )
        {
        }
    }

    #region DbSets

    ShizouDbSet<TEntity> IShizouContext.Set<TEntity>() => base.Set<TEntity>();
    ShizouDbSet<TEntity> IShizouContext.Set<TEntity>(string name) => base.Set<TEntity>(name);

    public ShizouDbSet<AniDbAnime> AniDbAnimes => Set<AniDbAnime>();
    public ShizouDbSet<AniDbAnimeRelation> AniDbAnimeRelations => Set<AniDbAnimeRelation>();
    public ShizouDbSet<AniDbCharacter> AniDbCharacters => Set<AniDbCharacter>();
    public ShizouDbSet<AniDbCreator> AniDbCreators => Set<AniDbCreator>();
    public ShizouDbSet<AniDbCredit> AniDbCredits => Set<AniDbCredit>();
    public ShizouDbSet<AniDbEpisode> AniDbEpisodes => Set<AniDbEpisode>();
    public ShizouDbSet<AniDbEpisodeFileXref> AniDbEpisodeFileXrefs => Set<AniDbEpisodeFileXref>();
    public ShizouDbSet<AniDbFile> AniDbFiles => Set<AniDbFile>();
    public ShizouDbSet<AniDbGenericFile> AniDbGenericFiles => Set<AniDbGenericFile>();
    public ShizouDbSet<AniDbGroup> AniDbGroups => Set<AniDbGroup>();
    public ShizouDbSet<AniDbNormalFile> AniDbNormalFiles => Set<AniDbNormalFile>();
    public ShizouDbSet<AnimeFilter> AnimeFilters => Set<AnimeFilter>();
    public ShizouDbSet<CommandRequest> CommandRequests => Set<CommandRequest>();
    public ShizouDbSet<FileWatchedState> FileWatchedStates => Set<FileWatchedState>();
    public ShizouDbSet<HangingEpisodeFileXref> HangingEpisodeFileXrefs => Set<HangingEpisodeFileXref>();
    public ShizouDbSet<ImportFolder> ImportFolders => Set<ImportFolder>();
    public ShizouDbSet<LocalFile> LocalFiles => Set<LocalFile>();
    public ShizouDbSet<LocalFileAttachment> LocalFileAttachments => Set<LocalFileAttachment>();
    public ShizouDbSet<MalAniDbXref> MalAniDbXrefs => Set<MalAniDbXref>();
    public ShizouDbSet<MalAnime> MalAnimes => Set<MalAnime>();
    public ShizouDbSet<ScheduledCommand> ScheduledCommands => Set<ScheduledCommand>();
    public ShizouDbSet<Timer> Timers => Set<Timer>();

    #endregion
}
