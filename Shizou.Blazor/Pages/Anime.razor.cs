using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Pages;

public partial class Anime
{
    [Parameter]
    public int AnimeId { get; set; }

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;

    private AniDbAnime? _anime;

    private readonly Regex _splitRegex = new(@"(?<=https?:\/\/\S*? \[.*?\])|(?=https?:\/\/\S*? \[.*?\])", RegexOptions.Compiled);
    private readonly Regex _matchRegex = new(@"(https?:\/\/\S*?) \[(.*?)\]", RegexOptions.Compiled);

    private readonly Dictionary<int, bool> _episodeExpanded = new();

    private readonly HashSet<LocalFile> _localFiles = new();
    private readonly HashSet<AniDbFile> _files = new();

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.Include(a => a.AniDbEpisodes).FirstOrDefault(a => a.Id == AnimeId);
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

    private List<LocalFile> GetManualLinksForEpisode(AniDbEpisode ep)
    {
        using var context = ContextFactory.CreateDbContext();
        context.AttachRange(_localFiles);
        var res = context.LocalFiles.Include(lf => lf.ManualLinkXrefs).Where(lf => lf.ManualLinkXrefs.Any(x => x.AniDbEpisodeId == ep.Id)).ToList();
        _localFiles.UnionWith(res);
        return res;
    }

    private List<(AniDbFile, LocalFile)> GetFilesForEpisode(AniDbEpisode ep)
    {
        using var context = ContextFactory.CreateDbContext();
        context.AttachRange(_files);
        context.AttachRange(_localFiles);
        var res = context.FilesFromEpisode(ep.Id).Include(f => f.AniDbGroup)
            .Join(context.LocalFiles, f => f.Ed2K, lf => lf.Ed2K,
                (f, lf) => new { f, lf }).ToList()
            .Select(f => (f.f, f.lf)).ToList();
        _localFiles.UnionWith(res.Select(r => r.lf));
        _files.UnionWith(res.Select(r => r.f));
        return res;
    }
}
