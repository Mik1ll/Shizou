using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Features.Anime.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime;

public partial class Anime
{
    private readonly Regex _matchRegex = new(@"(https?:\/\/\S*?) \[(.*?)\]", RegexOptions.Compiled);

    private readonly Regex _splitRegex = new(@"(?<=https?:\/\/\S*? \[.*?\])|(?=https?:\/\/\S*? \[.*?\])", RegexOptions.Compiled);

    private AniDbAnime? _anime;
    private EpisodeTable _episodeTable = default!;
    private bool _showEpisodes = true;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    public int AnimeId { get; set; }

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.AsSplitQuery()
            .Include(a => a.AniDbEpisodes)
            .ThenInclude(e => e.ManualLinkLocalFiles)
            .Include(a => a.MalAnimes)
            .FirstOrDefault(a => a.Id == AnimeId);
    }

    private void MarkAllWatched(AniDbAnime anime)
    {
        if (WatchStateService.MarkAnime(anime.Id, true))
            ToastDisplay.AddToast("Success", "Anime files marked watched", ToastStyle.Success);
        else
            ToastDisplay.AddToast("Error", "Something went wrong while marking anime files watched", ToastStyle.Error);
        _episodeTable.Reload();
    }
}
