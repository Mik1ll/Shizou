﻿using System.Text.Json;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class UpNext
{
    private AniDbEpisode? _episode;
    private LocalFile? _localFile;
    private FileWatchedState? _watchedState;
    private string _externalPlaybackUrl = default!;
    private bool _schemeHandlerInstalled;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    private IModalService ModalService { get; set; } = default!;

    [CascadingParameter(Name = "IdentityCookie")]
    private string IdentityCookie { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public AniDbAnime AniDbAnime { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public EventCallback OnChanged { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        Update();

        var res = await JsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.SchemeHandlerInstalled);
        _schemeHandlerInstalled = res is { ValueKind: JsonValueKind.String } && res.GetString() == "true";
        var baseUri = new Uri(NavigationManager.BaseUri);
        if (_localFile is not null)
            _externalPlaybackUrl = await ExternalPlaybackService.GetExternalPlaylistUriAsync(_localFile.Ed2k, false, baseUri, IdentityCookie);
    }

    private void Update()
    {
        _episode = AniDbAnime.AniDbEpisodes
            .Where(e => e.AniDbFiles.Any(f => f.LocalFiles.Any()) && e.AniDbFiles.All(f => f.FileWatchedState.Watched == false))
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
        WatchStateService.MarkFile(_watchedState!.AniDbFileId, watched);
        _ = OnChanged.InvokeAsync();
    }

    private async Task OpenVideoAsync(int localFileId)
    {
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), localFileId)).Result;
    }


    private async Task OpenSchemeHandlerDownloadModalAsync()
    {
        await ModalService.Show<SchemeHandlerDownloadModal>().Result;
        var res = await JsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.SchemeHandlerInstalled);
        _schemeHandlerInstalled = res is { ValueKind: JsonValueKind.String } && res.GetString() == "true";
    }
}
