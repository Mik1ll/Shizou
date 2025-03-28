﻿using System.Dynamic;
using System.Text.Json;
using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class FileCard
{
    private FileWatchedState _watchedState = default!;
    private string _externalPlaybackUrl = default!;
    private string _fileDownloadUrl = default!;
    private bool _fileExists;
    private bool _schemeHandlerInstalled;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = default!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [CascadingParameter(Name = "IdentityCookie")]
    public string IdentityCookie { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public LocalFile LocalFile { get; set; } = default!;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    protected override void OnInitialized()
    {
        CheckFileExists();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (LocalFile.AniDbFile is null)
            throw new ArgumentException("Must have either AniDb file or Manual Link");
        _watchedState = LocalFile.AniDbFile.FileWatchedState;

        var res = await JsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.SchemeHandlerInstalled);
        _schemeHandlerInstalled = res is { ValueKind: JsonValueKind.String } && res.GetString() == "true";

        var baseUri = new Uri(NavigationManager.BaseUri);
        _externalPlaybackUrl = await ExternalPlaybackService.GetExternalPlaylistUriAsync(LocalFile.Ed2k, true, baseUri, IdentityCookie);
        IDictionary<string, object?> values = new ExpandoObject();
        values["ed2K"] = LocalFile.Ed2k;
        values[IdentityConstants.ApplicationScheme] = IdentityCookie;
        _fileDownloadUrl = LinkGenerator.GetUriByAction(nameof(FileServer.Get), nameof(FileServer), values, baseUri.Scheme, new HostString(baseUri.Authority),
            new PathString(baseUri.AbsolutePath)) ?? throw new ArgumentException("Failed to generate file download uri");
    }

    private void CheckFileExists() =>
        _fileExists = LocalFile.ImportFolder is not null && File.Exists(Path.Combine(LocalFile.ImportFolder.Path, LocalFile.PathTail));

    private void ToggleWatched()
    {
        var watched = !_watchedState.Watched;
        WatchStateService.MarkFile(_watchedState.AniDbFileId, watched);

        _ = OnChanged.InvokeAsync();
    }

    private void RemoveLocalFile()
    {
        ImportService.RemoveLocalFile(LocalFile.Id);
        _ = OnChanged.InvokeAsync();
    }

    private void Unlink()
    {
        ManualLinkService.UnlinkFile(LocalFile.Id);
        _ = OnChanged.InvokeAsync();
    }

    private async Task OpenVideoAsync() =>
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), LocalFile.Id)).Result;

    private void Hash() => CommandService.Dispatch(new HashArgs(Path.Combine(LocalFile.ImportFolder!.Path, LocalFile.PathTail)));

    private void Scan() => CommandService.Dispatch(new ProcessArgs(LocalFile.Id, IdTypeLocalOrFile.LocalId));

    private async Task OpenSchemeHandlerDownloadModalAsync()
    {
        await ModalService.Show<SchemeHandlerDownloadModal>().Result;
        var res = await JsRuntime.InvokeAsync<JsonElement>("window.localStorage.getItem", LocalStorageKeys.SchemeHandlerInstalled);
        _schemeHandlerInstalled = res is { ValueKind: JsonValueKind.String } && res.GetString() == "true";
    }
}
