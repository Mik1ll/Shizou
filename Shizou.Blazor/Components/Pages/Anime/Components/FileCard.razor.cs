using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Blazor.Services;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class FileCard
{
    private FileWatchedState _watchedState = default!;
    private string _externalPlaybackUrl = default!;
    private bool _fileExists;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private ExternalPlaybackService ExternalPlaybackService { get; set; } = default!;

    [Inject]
    private CommandService CommandService { get; set; } = default!;

    [Inject]
    private ImportService ImportService { get; set; } = default!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = default!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

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
        _watchedState = LocalFile.AniDbFile?.FileWatchedState ?? throw new ArgumentNullException(nameof(FileWatchedState));

        _externalPlaybackUrl = await ExternalPlaybackService.GetExternalPlaylistUriAsync(LocalFile.Ed2k, true);
    }

    private void CheckFileExists() =>
        _fileExists = LocalFile.ImportFolder is not null && File.Exists(Path.Combine(LocalFile.ImportFolder.Path, LocalFile.PathTail));

    private async Task ToggleWatchedAsync()
    {
        var watched = !_watchedState.Watched;
        if (WatchStateService.MarkFile(_watchedState.AniDbFileId, watched))
            _watchedState.Watched = watched;

        await OnChanged.InvokeAsync();
    }

    private async Task RemoveLocalFileAsync()
    {
        ImportService.RemoveLocalFile(LocalFile.Id);
        await OnChanged.InvokeAsync();
    }

    private async Task UnlinkAsync()
    {
        ManualLinkService.UnlinkFile(LocalFile.Id);
        await OnChanged.InvokeAsync();
    }

    private async Task OpenVideoAsync() =>
        await ModalService.Show<VideoModal>(string.Empty, new ModalParameters().Add(nameof(VideoModal.LocalFileId), LocalFile.Id)).Result;

    private void Hash() => CommandService.Dispatch(new HashArgs(Path.Combine(LocalFile.ImportFolder!.Path, LocalFile.PathTail)));

    private void Scan() => CommandService.Dispatch(new ProcessArgs(LocalFile.Id, IdTypeLocalOrFile.LocalId));
}
