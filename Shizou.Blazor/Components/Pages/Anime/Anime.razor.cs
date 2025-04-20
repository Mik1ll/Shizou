using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime;

public partial class Anime
{
    [GeneratedRegex(@"(https?:\/\/\S*?) \[(.+?)\]")]
    private static partial Regex LinkRegex();

    private AniDbAnime? _anime;
    private List<(RelatedAnimeType, AniDbAnime)>? _relatedAnime;
    private string[] _splitDescription = null!;
    private string _posterPath = null!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = null!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [Inject]
    private CommandService CommandService { get; set; } = null!;

    [Inject]
    private ToastService ToastService { get; set; } = null!;

    [Parameter]
    public int AnimeId { get; set; }

    protected override void OnParametersSet()
    {
        _posterPath = GetPosterPath(AnimeId);
        Load();
        _splitDescription = LinkRegex().Split(_anime?.Description ?? "");
    }

    private string GetPosterPath(int animeId) =>
        // ReSharper disable once RedundantAnonymousTypePropertyName
        LinkGenerator.GetPathByAction(nameof(Images.GetAnimePoster), nameof(Images), new { animeId = animeId }) ??
        throw new ArgumentException("Could not generate anime poster path");

    private void Load()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.AsNoTracking().AsSplitQuery()
            .Include(a => a.MalAnimes)
            .Include(a => a.AniDbEpisodes).ThenInclude(e => e.AniDbFiles).ThenInclude(f => f.LocalFiles).ThenInclude(lf => lf.ImportFolder)
            .Include(a => a.AniDbEpisodes).ThenInclude(e => e.AniDbFiles).ThenInclude(f => f.FileWatchedState)
            .Include(a => a.AniDbEpisodes).ThenInclude(e => e.AniDbFiles).ThenInclude(f => ((AniDbNormalFile)f).AniDbGroup)
            .FirstOrDefault(a => a.Id == AnimeId);
        _relatedAnime = (from ra in context.AniDbAnimeRelations
            where ra.AnimeId == AnimeId
            join a in context.AniDbAnimes.HasLocalFiles() on ra.ToAnimeId equals a.Id
            select new { ra.RelationType, a }).AsEnumerable().Select(x => (x.RelationType, x.a)).ToList();
    }

    private void MarkAllWatched()
    {
        if (WatchStateService.MarkAnime(AnimeId, true))
            ToastService.ShowSuccess("Success", "Anime files marked watched");
        else
            ToastService.ShowError("Error", "Something went wrong while marking anime files watched");
        Load();
    }

    private void RefreshAnime()
    {
        CommandService.Dispatch(new AnimeArgs(AnimeId));
        ToastService.ShowInfo("Info", "Anime queued for refresh, check again after completed");
    }
}
