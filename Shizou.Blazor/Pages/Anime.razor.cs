using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Pages;

public partial class Anime
{
    [Parameter] public int AnimeId { get; set; }

    [Inject] public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject] public WatchStateService WatchStateService { get; set; } = default!;

    private AniDbAnime? _anime;

    private readonly Regex _splitRegex = new(@"(?<=https?:\/\/\S*? \[.*?\])|(?=https?:\/\/\S*? \[.*?\])", RegexOptions.Compiled);
    private readonly Regex _matchRegex = new(@"(https?:\/\/\S*?) \[(.*?)\]", RegexOptions.Compiled);

    private readonly Dictionary<int, bool> _episodeExpanded = new();

    private Dictionary<int, List<(AniDbFile File, LocalFile LocalFile)>> _files = new();

    private int? _videoOpen;

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.AsSingleQuery().Include(a => a.AniDbEpisodes).ThenInclude(e => e.ManualLinkLocalFiles)
            .FirstOrDefault(a => a.Id == AnimeId);
        _files = (from f in context.AniDbFiles.Include(f => f.AniDbGroup).AsSingleQuery()
            join lf in context.LocalFiles
                on f.Ed2K equals lf.Ed2K
            join xref in context.AniDbEpisodeFileXrefs
                on f.Id equals xref.AniDbFileId
            join ep in context.AniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == AnimeId
            group new { f, lf } by ep.Id).ToDictionary(g => g.Key, g => g.Select(x => (File: x.f, LocalFile: x.lf)).ToList());
    }

    private void MarkEpisode(AniDbEpisode ep, bool watched)
    {
        if (WatchStateService.MarkEpisode(ep.Id, watched))
            ep.Watched = watched;
    }

    private void MarkFile(AniDbFile f, bool watched)
    {
        if (WatchStateService.MarkFile(f.Id, watched))
            f.Watched = watched;
    }

    private void ToggleEpExpand(AniDbEpisode ep)
    {
        if (_episodeExpanded.TryGetValue(ep.Id, out var expanded))
            _episodeExpanded[ep.Id] = !expanded;
        else
            _episodeExpanded[ep.Id] = true;
    }

    private void MarkAllWatched(AniDbAnime anime)
    {
        if (WatchStateService.MarkAnime(anime.Id, true))
        {
            foreach (var f in _files.Values.SelectMany(i => i).Select(i => i.File).Distinct())
                f.Watched = true;
            foreach (var ep in anime.AniDbEpisodes.Where(e => e.ManualLinkLocalFiles.Any()))
                ep.Watched = true;
        }
    }

    private void OpenVideo(int localFileId)
    {
        _videoOpen = localFileId;
    }

    private void CloseVideo()
    {
        _videoOpen = null;
    }
    
}
