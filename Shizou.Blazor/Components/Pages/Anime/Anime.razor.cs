using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Extensions;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime;

public partial class Anime
{
    private static readonly Regex SplitRegex = new(@"(https?:\/\/\S*?) \[(.+?)\]", RegexOptions.Compiled);
    private readonly int _maxDescriptionLength = 500;

    private AniDbAnime? _anime;
    private List<(RelatedAnimeType, AniDbAnime)>? _relatedAnime;
    private string[] _splitDescription = default!;
    private string _posterPath = default!;
    private RenderFragment? _description;
    private bool _expandDescription;
    private bool _descriptionTooLong;

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
        Load();
        _splitDescription = SplitRegex.Split(_anime?.Description ?? "");
        _descriptionTooLong = string.Join(string.Empty, _splitDescription.Where((_, i) => i % 3 == 0)).Length > _maxDescriptionLength;
        _description = GenerateDescription();
    }

    private RenderFragment GenerateDescription()
    {
        return b =>
        {
            var count = 0;
            for (var i = 0; i < _splitDescription.Length; i++)
                if (i % 3 == 0)
                {
                    if (_expandDescription)
                    {
                        b.AddContent(0, _splitDescription[i]);
                    }
                    else
                    {
                        b.AddContent(1, _splitDescription[i].TruncateWithSuffix(_maxDescriptionLength - count, "..."));
                        if ((count += _splitDescription[i].Length) > _maxDescriptionLength)
                            break;
                    }
                }
                else if (i % 3 == 1)
                    b.AddMarkupContent(2, $"<a href=\"{_splitDescription[i]}\" target=\"_blank\">{_splitDescription[i + 1]}</a>");
        };
    }

    private void ToggleDescriptionExpand()
    {
        _expandDescription = !_expandDescription;
    }

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
