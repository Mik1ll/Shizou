using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class EpisodeTable
{
    private readonly Dictionary<int, bool> _episodeExpanded = new();
    private List<AniDbEpisode> _episodes = default!;
    private Dictionary<int, int> _fileCounts = default!;
    private HashSet<int> _watchedEps = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }

    public void Load()
    {
        foreach (var (epId, _) in _episodeExpanded.Where(ep => ep.Value))
            LoadFiles(_episodes.First(ep => ep.Id == epId));
        using var context = ContextFactory.CreateDbContext();
        _fileCounts = (from ep in context.AniDbEpisodes
                where ep.AniDbAnimeId == AnimeId
                select new { EpId = ep.Id, Count = ep.AniDbFiles.SelectMany(f => f.LocalFiles).Count() })
            .ToDictionary(x => x.EpId, x => x.Count);
        _watchedEps = (from ep in context.AniDbEpisodes
            where ep.AniDbAnimeId == AnimeId && ep.AniDbFiles.Any(f => f.FileWatchedState.Watched)
            select ep.Id).ToHashSet();
    }

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _episodes = (from ep in context.AniDbEpisodes.AsNoTracking()
            where ep.AniDbAnimeId == AnimeId
            select ep).OrderBy(e => e.EpisodeType).ThenBy(e => e.Number).ToList();
        Load();
    }

    private void ToggleEpExpand(AniDbEpisode ep)
    {
        if (_episodeExpanded.TryGetValue(ep.Id, out var expanded))
            _episodeExpanded[ep.Id] = !expanded;
        else
            _episodeExpanded[ep.Id] = true;
        if (_episodeExpanded[ep.Id])
            LoadFiles(ep);
    }

    private void LoadFiles(AniDbEpisode episode)
    {
        using var context = ContextFactory.CreateDbContext();
        episode.AniDbFiles = context.AniDbFiles.AsNoTracking().AsSingleQuery()
            .Where(f => f.AniDbEpisodeFileXrefs.Any(xr => xr.AniDbEpisodeId == episode.Id))
            .Include(f => f.LocalFiles)
            .ThenInclude(lf => lf.ImportFolder)
            .Include(f => ((AniDbNormalFile)f).AniDbGroup)
            .Include(f => f.FileWatchedState).ToList();
    }

    private string GetEpisodeThumbnailPath(int episodeId)
    {
        return LinkGenerator.GetPathByAction(nameof(Images.GetEpisodeThumbnail), nameof(Images), new { EpisodeId = episodeId }) ??
               throw new InvalidOperationException();
    }
}
