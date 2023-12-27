﻿using System.Dynamic;
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
    private IWatchedState _watchedState = default!;

    [Inject]
    private WatchStateService WatchStateService { get; set; } = default!;

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
    public LocalFile LocalFile { get; set; } = default!;

    [Parameter]
    public EventCallback OnChanged { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(IdentityCookie));
        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        if (LocalFile.ImportFolder is null || !File.Exists(Path.Combine(LocalFile.ImportFolder.Path, LocalFile.PathTail)))
            throw new ArgumentException(nameof(LocalFile.ImportFolder));
        if (LocalFile.AniDbFile is null && LocalFile.ManualLinkEpisode is null)
            throw new ArgumentException("Must have either AniDb file or Manual Link");
        if (LocalFile.AniDbFile?.FileWatchedState is null && LocalFile.ManualLinkEpisode?.EpisodeWatchedState is null)
            throw new ArgumentException(nameof(IWatchedState));
        _watchedState = (IWatchedState?)LocalFile.AniDbFile?.FileWatchedState ?? LocalFile.ManualLinkEpisode!.EpisodeWatchedState;
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

    private string PlayExternalPlaylist(bool single)
    {
        IDictionary<string, object?> values = new ExpandoObject();
        values["localFileId"] = $"{LocalFile.Id}.m3u8";
        values["single"] = single;
        values[Constants.IdentityCookieName] = IdentityCookie;
        var fileUri = LinkGenerator.GetUriByAction(HttpContextAccessor.HttpContext ?? throw new InvalidOperationException(), nameof(FileServer.GetWithPlaylist),
            nameof(FileServer), values) ?? throw new ArgumentException();
        return $"shizou:{fileUri}";
    }
}
