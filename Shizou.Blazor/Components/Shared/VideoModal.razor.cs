﻿using System.Net.Mime;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Shizou.Blazor.Extensions;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Controllers;
using Shizou.Server.Services;

namespace Shizou.Blazor.Components.Shared;

public partial class VideoModal
{
    private readonly string _videoId = "videoModalId";
    private readonly List<(string Url, string? Lang, string? Title)> _assSubs = [];
    private readonly List<string> _fontUrls = [];
    private bool _loadPlayer;
    private LocalFile? _localFile;
    private string? _localFileMimeType;
    private IJSObjectReference? _player;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = null!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = null!;

    [Inject]
    private FfmpegService FfmpegService { get; set; } = null!;

    [Parameter]
    public int LocalFileId { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(LocalFileId));
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnInitializedAsync()
    {
        using var context = ContextFactory.CreateDbContext();
        _localFile = context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == LocalFileId);
        if (_localFile is not null)
        {
            if (!new FileExtensionContentTypeProvider().TryGetContentType(_localFile.PathTail, out _localFileMimeType))
                _localFileMimeType = MediaTypeNames.Application.Octet;
            if (Path.GetExtension(_localFile.PathTail) == ".mkv") // Lie so browser will try to interpret mkv as webm for web player
                _localFileMimeType = "video/webm";
        }

        await GetStreamUrlsAsync();
        _loadPlayer = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadPlayer)
        {
            var module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "/js/webplayer.js");
            _player = await module.InvokeAsync<IJSObjectReference>("newPlayer", _videoId, _assSubs.Select(s => s.Url).ToList(), _fontUrls);
            _loadPlayer = false;
        }
    }

    private async Task DisposeJavascriptAsync()
    {
        if (_player is not null)
            await _player.InvokeVoidAsync("dispose");
    }

    private async Task GetStreamUrlsAsync()
    {
        if (_localFile is null || _localFile.ImportFolder is null)
            return;
        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(_localFile.ImportFolder.Path, _localFile.PathTail)));
        if (!fileInfo.Exists)
            return;
        _assSubs.Clear();
        _fontUrls.Clear();

        var streams = await FfmpegService.GetStreamsAsync(fileInfo);

        foreach (var stream in streams)
            if (SubtitleService.ValidSubFormats.Contains(stream.codec))
            {
                var subUrl = LinkGenerator.GetPathByAction(nameof(FileServer.GetSubtitle), nameof(FileServer),
                    // ReSharper disable once RedundantAnonymousTypePropertyName
                    new { ed2k = _localFile.Ed2k, index = stream.index }) ?? throw new ArgumentException("Could not generate subtitle path");
                _assSubs.Add((subUrl, stream.lang, stream.title));
            }
            else if (stream.filename is not null && (SubtitleService.ValidFontFormats.Contains(stream.codec) ||
                                                     SubtitleService.ValidFontFormats.Any(f =>
                                                         stream.filename.EndsWith(f, StringComparison.OrdinalIgnoreCase))))
            {
                var fontUrl = LinkGenerator.GetPathByAction(nameof(FileServer.GetFont), nameof(FileServer),
                    new { ed2k = _localFile.Ed2k, fontName = stream.filename }) ?? throw new ArgumentException("Could not generate font path");
                _fontUrls.Add(fontUrl);
            }
    }

    private string GetVideoPath() => LinkGenerator.GetPathByAction(nameof(FileServer.Get), nameof(FileServer), new { ed2k = _localFile?.Ed2k }) ??
                                     throw new ArgumentException("Could not generate video file path");
}
