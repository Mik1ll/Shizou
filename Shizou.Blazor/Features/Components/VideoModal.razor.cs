using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Web;
using Blazored.Modal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Shizou.Blazor.Extensions;
using Shizou.Data.Database;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Components;

public partial class VideoModal
{
    private readonly string _videoId = "videoModalId";
    private readonly List<(string Url, string? Lang, string? Title)> _assSubs = new();
    private readonly List<string> _fontUrls = new();
    private bool _loadSubtitles;
    private LocalFile? _localFile;
    private string? _localFileMimeType;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private IDbContextFactory<ShizouContext> ContextFactory { get; set; } = default!;

    [CascadingParameter]
    private BlazoredModalInstance ModalInstance { get; set; } = default!;

    [Parameter]
    public int LocalFileId { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.EnsureParametersSet(nameof(LocalFileId));
        return base.SetParametersAsync(parameters);
    }

    protected override async Task OnInitializedAsync()
    {
        // ReSharper disable once MethodHasAsyncOverload
        // ReSharper disable once UseAwaitUsing
        using var context = ContextFactory.CreateDbContext();
        _localFile = context.LocalFiles.Include(e => e.ImportFolder).FirstOrDefault(e => e.Id == LocalFileId);
        _localFileMimeType = null;
        if (_localFile is not null)
            new FileExtensionContentTypeProvider().TryGetContentType(_localFile.PathTail, out _localFileMimeType);
        _localFileMimeType ??= "video/mp4";
        await GetStreamUrls();
        _loadSubtitles = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadSubtitles)
        {
            await JsRuntime.InvokeVoidAsync("loadPlayer", _videoId, _assSubs.Select(s => s.Url).ToList(), _fontUrls);
            _loadSubtitles = false;
        }
    }

    private async Task Cancel()
    {
        await JsRuntime.InvokeVoidAsync("subtitleHandler.dispose");
        await ModalInstance.CancelAsync();
    }

    private async Task GetStreamUrls()
    {
        if (_localFile is null || _localFile.ImportFolder is null)
            return;
        var fileInfo = new FileInfo(Path.GetFullPath(Path.Combine(_localFile.ImportFolder.Path, _localFile.PathTail)));
        if (!fileInfo.Exists)
            return;

        using var p = new Process();
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.CreateNoWindow = true;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        p.StartInfo.FileName = "ffprobe";
        p.StartInfo.Arguments =
            $"-v fatal -show_entries \"stream=index,codec_name : stream_tags=language,title,filename\" -of json=c=1 \"{fileInfo.FullName}\"";
        p.Start();
        _assSubs.Clear();
        using var document = JsonDocument.Parse(await p.StandardOutput.ReadToEndAsync());
        foreach (var streamEl in document.RootElement.GetProperty("streams").EnumerateArray())
        {
            var index = streamEl.GetProperty("index").GetInt32();
            var codec = streamEl.GetProperty("codec_name")
                .GetString();
            if (new[] { "ass", "ssa", "srt", "webvtt", "subrip", "ttml", "text", "mov_text", "dvb_teletext" }.Contains(codec))
            {
                string? lang = null;
                string? title = null;
                if (streamEl.TryGetProperty("tags", out var tags))
                {
                    if (tags.TryGetProperty("language", out var langEl))
                        lang = langEl.GetString();
                    if (tags.TryGetProperty("title", out var titleEl))
                        title = titleEl.GetString();
                }

                _assSubs.Add(($"/api/FileServer/AssSubs/{LocalFileId}/{index}", lang, title));
            }
            else if (new[] { "ttf", "otf" }.Contains(codec))
            {
                string? filename = null;
                if (streamEl.TryGetProperty("tags", out var tags))
                    if (tags.TryGetProperty("filename", out var filenameEl))
                        filename = filenameEl.GetString();

                if (filename is not null)
                    _fontUrls.Add($"/api/FileServer/Fonts/{LocalFileId}/{index}?fontName={HttpUtility.UrlEncode(filename)}");
            }
        }
    }
}
