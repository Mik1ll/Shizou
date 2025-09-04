using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Components.Shared;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Pages.Anime.Components;

public partial class FileCard
{
    private FileWatchedState _watchedState = null!;
    private bool _fileExists;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = null!;

    [Inject]
    private CommandService CommandService { get; set; } = null!;

    [Inject]
    private ImportService ImportService { get; set; } = null!;

    [Inject]
    private ManualLinkService ManualLinkService { get; set; } = null!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public LocalFile LocalFile { get; set; } = null!;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    protected override void OnInitialized()
    {
        CheckFileExists();
    }

    protected override void OnParametersSet()
    {
        if (LocalFile.AniDbFile is null)
            throw new ArgumentException("Must have either AniDb file or Manual Link");
        _watchedState = LocalFile.AniDbFile.FileWatchedState;
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
}
