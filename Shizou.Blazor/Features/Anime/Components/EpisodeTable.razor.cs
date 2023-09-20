﻿using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class EpisodeTable
{
    private readonly Dictionary<int, bool> _episodeExpanded = new();
    private HashSet<AniDbEpisode> _episodes = default!;
    private HashSet<EpisodeWatchedState> _epWatchedStates = default!;
    private Dictionary<int, List<(AniDbFile, FileWatchedState?, LocalFile?)>> _files = new();
    private Dictionary<int, List<LocalFile>> _manualLinks = new();

    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }


    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    public void Reload()
    {
        foreach (var (epId, _) in _episodeExpanded.Where(ep => ep.Value))
        {
            _files[epId] = GetAniDbFiles(epId);
            _manualLinks[epId] = GetManualLinks(epId);
        }

        StateHasChanged();
    }
    
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
        if (_episodeExpanded[ep.Id])
        {
            _files[ep.Id] = GetAniDbFiles(ep.Id);
            _manualLinks[ep.Id] = GetManualLinks(ep.Id);
        }
    }

    private List<(AniDbFile, FileWatchedState?, LocalFile?)> GetAniDbFiles(int episodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        var result = (from f in context.AniDbFiles.Include(f => f.AniDbGroup)
            join xref in context.AniDbEpisodeFileXrefs on f.Id equals xref.AniDbFileId
            where xref.AniDbEpisodeId == episodeId
            join sublf in context.LocalFiles on f.Ed2k equals sublf.Ed2k into lflj
            from lf in lflj.DefaultIfEmpty()
            join subws in context.FileWatchedStates on f.Id equals subws.Id into wslj
            from ws in wslj.DefaultIfEmpty()
            select new { f, ws, lf }).ToList();
        return result.Select(x => (x.f, (FileWatchedState?)x.ws, (LocalFile?)x.lf)).ToList();
    }

    private List<LocalFile> GetManualLinks(int episodeId)
    {
        using var context = ContextFactory.CreateDbContext();
        var result = context.LocalFiles.Where(lf => lf.ManualLinkEpisodeId == episodeId).ToList();
        return result;
    }
}