using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class UpNext
{
    private AniDbEpisode? _episode;
    private LocalFile? _localFile;
    private FileWatchedState? _watchedState;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public AniDbAnime AniDbAnime { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public EventCallback OnChanged { get; set; }

    protected override void OnParametersSet()
    {
        _episode = AniDbAnime.AniDbEpisodes
            .Where(e => e.AniDbFiles.Any(f => f.LocalFiles.Count != 0) && e.AniDbFiles.All(f => !f.FileWatchedState.Watched))
            .OrderBy(e => e.EpisodeType).ThenBy(e => e.Number).FirstOrDefault();
        if (_episode is null)
            return;
        _localFile = _episode.AniDbFiles.SelectMany(f => f.LocalFiles).First();
        _watchedState = _localFile.AniDbFile!.FileWatchedState;
    }

    private string GetEpisodeThumbnailPath() =>
        LinkGenerator.GetPathByAction(nameof(Images.GetEpisodeThumbnail), nameof(Images), new { episodeId = _episode!.Id }) ??
        throw new ArgumentException("Could not generate episode thumbnail path");

    private void Mark(bool watched)
    {
        WatchStateService.MarkFile(_watchedState!.AniDbFileId, watched);
        _ = OnChanged.InvokeAsync();
    }

    private async Task OpenVideoAsync(int localFileId)
    {
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), localFileId)).Result;
    }
}
