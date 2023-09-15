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
    private Dictionary<int, MalAnime>? _malAnimes;
    private List<MalAniDbXref>? _malXrefs;

    [CascadingParameter]
    public ToastDisplay ToastDisplay { get; set; } = default!;

    [Parameter]
    public int AnimeId { get; set; }

    [Inject]
    public IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;

    protected override void OnInitialized()
    {
        using var context = ContextFactory.CreateDbContext();
        _anime = context.AniDbAnimes.AsSingleQuery().Include(a => a.AniDbEpisodes).ThenInclude(e => e.ManualLinkLocalFiles)
            .FirstOrDefault(a => a.Id == AnimeId);
        if (_anime is null)
            return;
        _malXrefs = context.MalAniDbXrefs.Where(x => x.AniDbId == _anime.Id).ToList();
        _malAnimes = (from malAnime in context.MalAnimes
            join xref in context.MalAniDbXrefs on malAnime.Id equals xref.MalId
            where xref.AniDbId == _anime.Id
            select malAnime).ToDictionary(a => a.Id);
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
            ToastDisplay.AddToast("Success", "Anime files marked watched", ToastStyle.Success);
        else
            ToastDisplay.AddToast("Error", "Something went wrong while marking anime files watched", ToastStyle.Error);
    }
}