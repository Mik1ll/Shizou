﻿using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Features.Components;
using Shizou.Data;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class FileCard
{
    private IWatchedState _watchedState = default!;


    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter(Name = nameof(App.IdentityCookie))]
    private string? IdentityCookie { get; set; }

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AniDbFile? AniDbFile { get; set; }

    [Parameter]
    [EditorRequired]
    public FileWatchedState? FileWatchedState { get; set; }

    [Parameter]
    [EditorRequired]
    public LocalFile? LocalFile { get; set; }

    [Parameter]
    [EditorRequired]
    public EpisodeWatchedState? EpisodeWatchedState { get; set; }


    protected override void OnParametersSet()
    {
        _watchedState = AniDbFile is null
            ? EpisodeWatchedState ?? throw new ArgumentNullException(nameof(EpisodeWatchedState))
            : FileWatchedState ?? throw new ArgumentNullException(nameof(FileWatchedState));
    }

    private void Mark(bool watched)
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
    }

    private async Task OpenVideo(int localFileId)
    {
        await ModalService.Show<ModalVideo>(string.Empty, new ModalParameters().Add(nameof(ModalVideo.LocalFileId), localFileId)).Result;
    }

    private void OpenInMpv(LocalFile file)
    {
        if (IdentityCookie is null)
            return;
        NavigationManager.NavigateTo(
            $"mpv:{NavigationManager.BaseUri}api/FileServer/{file.Id}{Path.GetExtension(file.PathTail)}?{Constants.IdentityCookieName}={IdentityCookie}");
    }
}
