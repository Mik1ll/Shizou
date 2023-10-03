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
    public EpisodeWatchedState? EpisodeWatchedState { get; set; }


    [Inject]
    public WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;


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
                if (WatchStateService.MarkFile(fws.Id, watched))
                    _watchedState.Watched = watched;
                break;
            case EpisodeWatchedState ews:
                if (WatchStateService.MarkEpisode(ews.Id, watched))
                    _watchedState.Watched = watched;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_watchedState));
        }
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
