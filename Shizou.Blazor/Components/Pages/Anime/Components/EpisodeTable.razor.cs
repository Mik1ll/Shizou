using Microsoft.AspNetCore.Components;
using Shizou.Data.Models;
using Shizou.Server.Controllers;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class EpisodeTable
{
    private readonly Dictionary<int, bool> _episodeExpanded = new();
    private Dictionary<int, int> _fileCounts = default!;
    private HashSet<int> _watchedEps = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AniDbAnime AniDbAnime { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public EventCallback OnChanged { get; set; }

    protected override void OnParametersSet()
    {
        Update();
    }

    private void Update()
    {
        _fileCounts = (from ep in AniDbAnime.AniDbEpisodes
                select new { EpId = ep.Id, Count = ep.AniDbFiles.SelectMany(f => f.LocalFiles).Count() })
            .ToDictionary(x => x.EpId, x => x.Count);
        _watchedEps = (from ep in AniDbAnime.AniDbEpisodes
            where ep.AniDbFiles.Any(f => f.FileWatchedState.Watched)
            select ep.Id).ToHashSet();
    }

    private void ToggleEpExpand(AniDbEpisode ep)
    {
        if (_episodeExpanded.TryGetValue(ep.Id, out var expanded))
            _episodeExpanded[ep.Id] = !expanded;
        else
            _episodeExpanded[ep.Id] = true;
    }


    private string GetEpisodeThumbnailPath(int episodeId)
    {
        return LinkGenerator.GetPathByAction(nameof(Images.GetEpisodeThumbnail), nameof(Images), new { EpisodeId = episodeId }) ??
               throw new InvalidOperationException();
    }
}
