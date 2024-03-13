using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Extensions.Query;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class UpNext
{
    private AniDbEpisode? _episode;
    private LocalFile? _localFile;
    private FileWatchedState? _watchedState;
    private string _externalPlaybackUrl = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = default!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public int AnimeId { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        Reload();
        if (_localFile is not null)
            _externalPlaybackUrl = await ExternalPlaybackService.GetExternalPlaylistUriAsync(_localFile.Ed2k, false);
    }

    private void Reload()
    {
        using var context = ContextFactory.CreateDbContext();
        _episode = context.AniDbEpisodes.Include(ep => ep.AniDbFiles).ThenInclude(f => f.LocalFiles)
            .Include(ep => ep.AniDbFiles).ThenInclude(f => f.FileWatchedState).AsSingleQuery().HasLocalFiles()
            .Where(e => e.AniDbAnimeId == AnimeId && e.AniDbFiles.All(f => f.FileWatchedState.Watched == false))
            .OrderBy(e => e.EpisodeType).ThenBy(e => e.Number).FirstOrDefault();
        if (_episode is null)
            return;
        _localFile = _episode.AniDbFiles.SelectMany(f => f.LocalFiles).First();
        _watchedState = _localFile.AniDbFile!.FileWatchedState;
    }


    private string GetEpisodeThumbnailPath() =>
        LinkGenerator.GetPathByAction(nameof(Images.GetEpisodeThumbnail), nameof(Images), new { EpisodeId = _episode!.Id }) ??
        throw new InvalidOperationException();

    private void Mark(bool watched)
    {
        if (WatchStateService.MarkFile(_watchedState!.AniDbFileId, watched))
            _watchedState.Watched = watched;

        Reload();
    }

    private async Task OpenVideoAsync(int localFileId)
    {
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), localFileId)).Result;
    }
}
