using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Pages.Anime.Components;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime;

public partial class Anime
{
    private readonly Regex _splitRegex = new(@"(https?:\/\/\S*?) \[(.+?)\]", RegexOptions.Compiled);

    private AniDbAnime? _anime;
    private EpisodeTable? _episodeTable;
    private List<(RelatedAnimeType, AniDbAnime)>? _relatedAnime;
    private string[] _splitDescription = default!;
    private string _posterPath = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Parameter]
    public int AnimeId { get; set; }

    protected override void OnParametersSet()
    {
        _posterPath = LinkGenerator.GetPathByAction(nameof(Images.GetAnimePoster), nameof(Images), new { AnimeId }) ?? throw new ArgumentException();
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes
            .Include(a => a.MalAnimes)
            .FirstOrDefault(a => a.Id == AnimeId);
        _relatedAnime = (from ra in context.AniDbAnimeRelations
            where ra.AnimeId == AnimeId
            join a in context.AniDbAnimes on ra.ToAnimeId equals a.Id
            select new { ra.RelationType, a }).AsEnumerable().Select(x => (x.RelationType, x.a)).ToList();
        _splitDescription = _splitRegex.Split(_anime?.Description ?? "");
    }

    private void MarkAllWatched()
    {
        if (WatchStateService.MarkAnime(AnimeId, true))
            ToastService.ShowSuccess("Success", "Anime files marked watched");
        else
            ToastService.ShowError("Error", "Something went wrong while marking anime files watched");
        _episodeTable?.Load();
    }

    private void RefreshAnime()
    {
        CommandService.Dispatch(new AnimeArgs(AnimeId));
        ToastService.ShowInfo("Info", "Anime queued for refresh, check again after completed");
    }
}
