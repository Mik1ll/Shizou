using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime;

public partial class Anime
{
    [Parameter]
    public int AnimeId { get; set; }

    [CascadingParameter(Name = "IdentityCookie")]
    public string? IdentityCookie { get; set; }

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public MyAnimeListService MyAnimeListService { get; set; } = default!;

    private AniDbAnime? _anime;
    private ILookup<int, AniDbFile>? _filesGroupedByEpId;
    private HashSet<LocalFile>? _regularLocalFiles;
    private HashSet<FileWatchedState>? _fileWatchedStates;
    private HashSet<EpisodeWatchedState>? _epWatchedStates;
    private List<MalAniDbXref>? _malXrefs;
    private Dictionary<int, MalAnime>? _malAnimes;

    private readonly Regex _splitRegex = new(@"(?<=https?:\/\/\S*? \[.*?\])|(?=https?:\/\/\S*? \[.*?\])", RegexOptions.Compiled);
    private readonly Regex _matchRegex = new(@"(https?:\/\/\S*?) \[(.*?)\]", RegexOptions.Compiled);

    private readonly Dictionary<int, bool> _episodeExpanded = new();

    private int? _videoOpen;

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.AsSingleQuery().Include(a => a.AniDbEpisodes).ThenInclude(e => e.ManualLinkLocalFiles)
            .FirstOrDefault(a => a.Id == AnimeId);
        if (_anime is null)
            return;
        var filesQuery = from f in context.AniDbFiles.Include(f => f.AniDbGroup)
            join xref in context.AniDbEpisodeFileXrefs
                on f.Id equals xref.AniDbFileId
            join ep in context.AniDbEpisodes
                on xref.AniDbEpisodeId equals ep.Id
            where ep.AniDbAnimeId == _anime.Id
            select new { EpId = ep.Id, File = f };
        _filesGroupedByEpId = filesQuery.ToLookup(x => x.EpId, x => x.File);
        _regularLocalFiles = (from lf in context.LocalFiles
            join f in filesQuery.Select(x => x.File)
                on lf.Ed2k equals f.Ed2k
            select lf).ToHashSet();
        _fileWatchedStates = (from ws in context.FileWatchedStates
            join f in filesQuery.Select(x => x.File)
                on ws.Id equals f.Id
            select ws).ToHashSet();
        _epWatchedStates = (from ws in context.EpisodeWatchedStates
            join ep in context.AniDbEpisodes
                on ws.Id equals ep.Id
            where ep.AniDbAnimeId == _anime.Id
            select ws).ToHashSet();
        _malXrefs = context.MalAniDbXrefs.Where(x => x.AniDbId == _anime.Id).ToList();
        _malAnimes = (from malAnime in context.MalAnimes
            join xref in context.MalAniDbXrefs on malAnime.Id equals xref.MalId
            where xref.AniDbId == _anime.Id
            select malAnime).ToDictionary(a => a.Id);
    }

    private void MarkEpisode(EpisodeWatchedState watchedState, bool watched)
    {
        if (WatchStateService.MarkEpisode(watchedState.Id, watched))
            watchedState.Watched = watched;
    }

    private void MarkFile(FileWatchedState f, bool watched)
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
            foreach (var ws in _fileWatchedStates!)
                ws.Watched = true;
            foreach (var ws in from ws in _epWatchedStates
                     join ep in _anime!.AniDbEpisodes
                         on ws.Id equals ep.Id
                     where ep.ManualLinkLocalFiles.Any()
                     select ws)
                ws.Watched = true;
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

    private void OpenInMpv(LocalFile file)
    {
        if (IdentityCookie is null)
            return;
        NavigationManager.NavigateTo(
            $"mpv:{NavigationManager.BaseUri}api/FileServer/{file.Id}{Path.GetExtension(file.PathTail)}?{Constants.IdentityCookieName}={IdentityCookie}");
    }
}