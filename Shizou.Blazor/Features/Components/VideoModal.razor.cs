using System.Diagnostics;
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
    private readonly List<string> _assSubUrls = new();
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
        await GetAssStreamUrls();
        _loadSubtitles = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_loadSubtitles)
        {
            await JsRuntime.InvokeVoidAsync("loadPlayer", _videoId, _assSubUrls);
            _loadSubtitles = false;
        }
    }

    private async Task Cancel()
    {
        await JsRuntime.InvokeVoidAsync("subtitleHandler.dispose");
        await ModalInstance.CancelAsync();
    }

    private async Task GetAssStreamUrls()
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
        p.StartInfo.FileName = "ffprobe";
        p.StartInfo.Arguments = $"-v fatal -select_streams s -show_entries stream=index,codec_name -of csv=p=0 \"{fileInfo.FullName}\"";
        p.Start();
        _assSubUrls.Clear();
        while (await p.StandardOutput.ReadLineAsync() is { } line)
        {
            var split = line.Split(',');
            if (split[1] != "ass")
                continue;
            var index = int.Parse(split[0]);
            _assSubUrls.Add($"/api/FileServer/AssSubs/{LocalFileId}/{index}");
        }
    }
}
