using System.Dynamic;
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
    private FileWatchedState _watchedState = null!;
    private string _externalPlaybackUrl = null!;
    private string _fileDownloadUrl = null!;
    private bool _fileExists;
    private bool _schemeHandlerInstalled;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = null!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = null!;

    [Inject]
    private CommandService CommandService { get; set; } = null!;

    [Inject]
    private ImportService ImportService { get; set; } = null!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = null!;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = null!;

    [CascadingParameter(Name = "IdentityCookie")]
    public string IdentityCookie { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public LocalFile LocalFile { get; set; } = null!;

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

        _schemeHandlerInstalled = (await BrowserSettings.GetSettingsAsync(JsRuntime)).ExternalPlayerInstalled;

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
        _schemeHandlerInstalled = (await BrowserSettings.GetSettingsAsync(JsRuntime)).ExternalPlayerInstalled;
    }
}
