using Microsoft.AspNetCore.Components;
using Shizou.Data;
using Shizou.Data.Models;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class FileCard
{
    private int? _videoOpen;
    private IWatchedState _watchedState = default!;

    [CascadingParameter(Name = "IdentityCookie")]
    public string? IdentityCookie { get; set; }

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
    public EpisodeWatchedState EpisodeWatchedState { get; set; } = default!;

    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;


    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;


    protected override void OnInitialized()
    {
        _watchedState = FileWatchedState as IWatchedState ?? EpisodeWatchedState;
        base.OnInitialized();
    }

    private void Mark(bool watched)
    {
        switch (_watchedState)
        {
            case Data.Models.FileWatchedState:
                if (WatchStateService.MarkFile(_watchedState.Id, watched))
                    _watchedState.Watched = watched;
                break;
            case Data.Models.EpisodeWatchedState:
                if (WatchStateService.MarkEpisode(_watchedState.Id, watched))
                    _watchedState.Watched = watched;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_watchedState));
        }
    }

    private void MarkUnwatched()
    {
    }

    private void OpenVideo(int localFileId)
    {
        _videoOpen = localFileId;
    }

    private void CloseVideo()
    {
        _videoOpen = null;
    }

    private void OpenInMpv(LocalFile file)
    {
        if (IdentityCookie is null)
            return;
        NavigationManager.NavigateTo(
            $"mpv:{NavigationManager.BaseUri}api/FileServer/{file.Id}{Path.GetExtension(file.PathTail)}?{Constants.IdentityCookieName}={IdentityCookie}");
    }
}