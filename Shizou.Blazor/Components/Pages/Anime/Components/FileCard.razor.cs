﻿using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class FileCard
{
    private IWatchedState _watchedState = default!;
    private string _externalPlaybackUrl = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public LocalFile LocalFile { get; set; } = default!;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        if (LocalFile.ImportFolder is null)
            throw new ArgumentNullException(nameof(LocalFile.ImportFolder));
        if (LocalFile.AniDbFile is null && LocalFile.ManualLinkEpisode is null)
            throw new ArgumentException("Must have either AniDb file or Manual Link");
        if (LocalFile.AniDbFile?.FileWatchedState is null && LocalFile.ManualLinkEpisode?.EpisodeWatchedState is null)
            throw new ArgumentNullException(nameof(IWatchedState));
        _watchedState = (IWatchedState?)LocalFile.AniDbFile?.FileWatchedState ?? LocalFile.ManualLinkEpisode!.EpisodeWatchedState;

        _externalPlaybackUrl = await ExternalPlaybackService.GetExternalPlaylistUriAsync(LocalFile.Ed2k, true);
    }

    private Task MarkAsync(bool watched)
    {
        switch (_watchedState)
        {
            case FileWatchedState fws:
                if (WatchStateService.MarkFile(fws.AniDbFileId, watched))
                    _watchedState.Watched = watched;
                break;
            case EpisodeWatchedState ews:
                if (WatchStateService.MarkEpisode(ews.AniDbEpisodeId, watched))
                    _watchedState.Watched = watched;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_watchedState));
        }

        return OnChanged.InvokeAsync();
    }

    private async Task UnlinkAsync(LocalFile localFile)
    {
        using var context = ContextFactory.CreateDbContext();
        context.LocalFiles.Attach(localFile);
        localFile.ManualLinkEpisodeId = null;

        context.SaveChanges();
        await OnChanged.InvokeAsync();
    }

    private async Task OpenVideoAsync(int localFileId)
    {
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), localFileId)).Result;
    }
}
