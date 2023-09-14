using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class EpisodeTable
{
    private readonly Dictionary<int, bool> _episodeExpanded = new();
    private HashSet<AniDbEpisode> _episodes = default!;
    private HashSet<EpisodeWatchedState> _epWatchedStates = default!;

    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }


    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        var epResult = (from ep in context.AniDbEpisodes
            where ep.AniDbAnimeId == AnimeId
            join ws in context.EpisodeWatchedStates on ep.Id equals ws.Id into wslj
            from ws in wslj.DefaultIfEmpty()
            select new { ep, ws }).ToList();
        _episodes = epResult.Select(r => r.ep).ToHashSet();
        _epWatchedStates = epResult.Select(r => r.ws).ToHashSet();
        base.OnInitialized();
    }


    private void ToggleEpExpand(AniDbEpisode ep)
    {
        if (_episodeExpanded.TryGetValue(ep.Id, out var expanded))
            _episodeExpanded[ep.Id] = !expanded;
        else
            _episodeExpanded[ep.Id] = true;
    }

    private List<(AniDbFile, FileWatchedState?, LocalFile?)> GetAniDbFiles(int episodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        var result = (from f in context.AniDbFiles.Include(f => f.AniDbGroup)
            join xref in context.AniDbEpisodeFileXrefs on f.Id equals xref.AniDbFileId
            where xref.AniDbEpisodeId == episodeId
            join lf in context.LocalFiles on f.Ed2k equals lf.Ed2k into lflj
            from lf in lflj.DefaultIfEmpty()
            join ws in context.FileWatchedStates on f.Id equals ws.Id into wslj
            from ws in wslj.DefaultIfEmpty()
            select new { f, ws, lf }).ToList();
        return result.Select(x => (x.f, x.ws, x.lf)).ToList();
    }

    private List<LocalFile> GetManualLinks(int episodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        var result = (from lf in context.LocalFiles
            join xref in context.ManualLinkXrefs on lf.Id equals xref.LocalFileId
            where xref.AniDbEpisodeId == episodeId
            select lf).ToList();
        return result;
    }
}