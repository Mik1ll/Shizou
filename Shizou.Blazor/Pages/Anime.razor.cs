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
}
