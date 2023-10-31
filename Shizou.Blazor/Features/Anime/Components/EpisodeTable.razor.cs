﻿using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class EpisodeTable
{
    private readonly Dictionary<int, bool> _episodeExpanded = new();
    private HashSet<AniDbEpisode> _episodes = default!;
    private Dictionary<int, int> _fileCounts = default!;
    private HashSet<int> _watchedEps = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }

    public void Reload()
    {
        foreach (var (epId, _) in _episodeExpanded.Where(ep => ep.Value))
            LoadEpisodeData(_episodes.First(ep => ep.Id == epId));
        using var context = ContextFactory.CreateDbContext();
        _fileCounts = (from ep in context.AniDbEpisodes
                where ep.AniDbAnimeId == AnimeId
                select new { EpId = ep.Id, Count = ep.AniDbFiles.Count(f => f.LocalFile != null) + ep.ManualLinkLocalFiles.Count })
            .ToDictionary(x => x.EpId, x => x.Count);
        _watchedEps = (from ep in context.AniDbEpisodes
            where ep.AniDbAnimeId == AnimeId && (ep.EpisodeWatchedState.Watched || ep.AniDbFiles.Any(f => f.FileWatchedState.Watched))
            select ep.Id).ToHashSet();
    }

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _episodes = (from ep in context.AniDbEpisodes
            where ep.AniDbAnimeId == AnimeId
            select ep).ToHashSet();
        Reload();
    }

    private void ToggleEpExpand(AniDbEpisode ep)
    {
        if (_episodeExpanded.TryGetValue(ep.Id, out var expanded))
            _episodeExpanded[ep.Id] = !expanded;
        else
            _episodeExpanded[ep.Id] = true;
        if (_episodeExpanded[ep.Id])
            LoadEpisodeData(ep);
    }

    private void LoadEpisodeData(AniDbEpisode episode)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Attach(episode).Collection(ep => ep.AniDbFiles)
            .Query().AsSplitQuery()
            .Include(f => f.LocalFile)
            .ThenInclude(lf => lf!.ImportFolder)
            .Include(f => f.AniDbGroup)
            .Include(f => f.FileWatchedState)
            .Load();
        context.Entry(episode).Collection(ep => ep.ManualLinkLocalFiles).Load();
        context.Entry(episode).Reference(ep => ep.EpisodeWatchedState).Load();
    }
}
