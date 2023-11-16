using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Features.Anime.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime;

public partial class Anime
{
    private readonly Regex _splitRegex = new(@"(https?:\/\/\S*?) \[(.+?)\]", RegexOptions.Compiled);

    private AniDbAnime? _anime;
    private EpisodeTable? _episodeTable;
    private List<(RelatedAnimeType, AniDbAnime)>? _relatedAnime;
    private string[] _splitDescription = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [CascadingParameter]
    private ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    public int AnimeId { get; set; }

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes
            .Include(a => a.MalAnimes)
            .FirstOrDefault(a => a.Id == AnimeId);
        _relatedAnime = (from ra in context.AniDbAnimeRelations
            where ra.AnimeId == AnimeId
            join a in context.AniDbAnimes.HasLocalFiles() on ra.ToAnimeId equals a.Id
            select new { ra.RelationType, a }).AsEnumerable().Select(x => (x.RelationType, x.a)).ToList();
        _splitDescription = _splitRegex.Split(_anime?.Description ?? "");
    }

    private void MarkAllWatched()
    {
        if (WatchStateService.MarkAnime(AnimeId, true))
            ToastDisplay.AddToast("Success", "Anime files marked watched", ToastStyle.Success);
        else
            ToastDisplay.AddToast("Error", "Something went wrong while marking anime files watched", ToastStyle.Error);
        _episodeTable?.Reload();
    }

    private void RefreshAnime()
    {
        CommandService.Dispatch(new AnimeArgs(AnimeId));
        ToastDisplay.AddToast("Info", "Anime queued for refresh, check again after completed", ToastStyle.Info);
    }
}
