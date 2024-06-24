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
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IShizouContextFactory ContextFactory { get; set; } = default!;

    [Inject]
    private LinkGenerator LinkGenerator { get; set; } = default!;

    [Inject]
    private FfmpegService FfmpegService { get; set; } = default!;

    [Inject]
    private IContentTypeProvider ContentTypeProvider { get; set; } = default!;

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
        _localFileMimeType = null;
        if (_localFile is not null)
            ContentTypeProvider.TryGetContentType(_localFile.PathTail, out _localFileMimeType);
        _localFileMimeType ??= "video/mp4";
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
                    new { _localFile.Ed2k, stream.index }) ?? throw new ArgumentException();
                _assSubs.Add((subUrl, stream.lang, stream.title));
            }
            else if (stream.filename is not null && (SubtitleService.ValidFontFormats.Contains(stream.codec) ||
                                                     SubtitleService.ValidFontFormats.Any(f =>
                                                         stream.filename.EndsWith(f, StringComparison.OrdinalIgnoreCase))))
            {
                var fontUrl = LinkGenerator.GetPathByAction(nameof(FileServer.GetFont), nameof(FileServer),
                    new { _localFile.Ed2k, FontName = stream.filename }) ?? throw new ArgumentException();
                _fontUrls.Add(fontUrl);
            }
    }
}
