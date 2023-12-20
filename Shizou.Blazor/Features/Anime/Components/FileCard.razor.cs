using Blazored.Modal;
using Blazored.Modal.Services;
using Microsoft.AspNetCore.Components;
using Shizou.Blazor.Extensions;
using Shizou.Blazor.Features.Components;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Features.Anime.Components;

public partial class FileCard
{
    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [CascadingParameter(Name = nameof(App.IdentityCookie))]
    private string IdentityCookie { get; set; } = default!;

    [CascadingParameter]
    public IModalService ModalService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public AniDbFile? AniDbFile { get; set; }

    [Parameter]
    [EditorRequired]
    public IWatchedState WatchedState { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public LocalFile LocalFile { get; set; } = default!;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(IdentityCookie));
        return base.SetParametersAsync(parameters);
    }

    private Task MarkAsync(bool watched)
    {
        switch (WatchedState)
        {
            case FileWatchedState fws:
                if (WatchStateService.MarkFile(fws.AniDbFileId, watched))
                    WatchedState.Watched = watched;
                break;
            case EpisodeWatchedState ews:
                if (WatchStateService.MarkEpisode(ews.AniDbEpisodeId, watched))
                    WatchedState.Watched = watched;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(WatchedState));
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

    private string PlayExternalUri()
    {
        var uri = LinkGenerator.GetUriByAction(HttpContextAccessor.HttpContext ?? throw new InvalidOperationException(), nameof(FileServer.Get),
            nameof(FileServer), new { LocalFileId = $"{LocalFile.Id}{Path.GetExtension(LocalFile.PathTail)}" }) ?? throw new ArgumentException();
        return $"shizou:{uri}?{Constants.IdentityCookieName}={IdentityCookie}";
    }
}
