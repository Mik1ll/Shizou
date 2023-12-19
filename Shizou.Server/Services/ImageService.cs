using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Options;

namespace Shizou.Server.Services;

public class ImageService
{
    private readonly ILogger<ImageService> _logger;
    private readonly IShizouContextFactory _contextFactory;
    private readonly IOptionsMonitor<ShizouOptions> _optionsMonitor;
    private readonly CommandService _commandService;
    private readonly FfmpegService _ffmpegService;


    public ImageService(
        ILogger<ImageService> logger,
        IShizouContextFactory contextFactory,
        IOptionsMonitor<ShizouOptions> optionsMonitor,
        CommandService commandService,
        FfmpegService ffmpegService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _optionsMonitor = optionsMonitor;
        _commandService = commandService;
        _ffmpegService = ffmpegService;
    }

    public void GetMissingAnimePosters()
    {
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        var filenames = context.AniDbAnimes.Where(a => a.ImageFilename != null).Select(a => a.ImageFilename!).ToList();

        foreach (var (uri, name) in filenames.Select(p => (GetAnimePosterUri(imageServer, p), p)))
        {
            var path = FilePaths.AnimePosterPath(name);
            if (!File.Exists(path))
                _commandService.Dispatch(new GetImageCommandArgs(uri, path));
        }
    }

    public void GetAnimePoster(int animeId)
    {
        var imageServer = _optionsMonitor.CurrentValue.AniDb.ImageServerHost;
        if (imageServer is null)
        {
            _logger.LogWarning("Image server is missing, aborting");
            return;
        }

        using var context = _contextFactory.CreateDbContext();
        var filename = context.AniDbAnimes.Where(a => a.Id == animeId).Select(a => a.ImageFilename).FirstOrDefault();
        if (filename is null)
        {
            _logger.LogWarning("Anime image for anime {AnimeId} does not exist, aborting", animeId);
            return;
        }

        var path = FilePaths.AnimePosterPath(filename);
        var uri = GetAnimePosterUri(imageServer, filename);
        _commandService.Dispatch(new GetImageCommandArgs(uri, path));
    }

    public async Task<FileInfo?> GetEpisodeThumbnailAsync(int episodeId)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFiles = context.LocalFiles.AsNoTracking().Include(lf => lf.ImportFolder).Where(lf =>
            lf.AniDbFile!.AniDbEpisodeFileXrefs.Any(ep => ep.AniDbEpisodeId == episodeId) || lf.ManualLinkEpisodeId == episodeId).ToList();
        foreach (var localFile in localFiles)
            if (new FileInfo(FilePaths.ExtraFileData.ThumbnailPath(localFile.Ed2k)) is { Exists: true } thumbnail)
                return thumbnail;

        foreach (var localFile in localFiles)
            if (await CreateThumbnailAsync(localFile).ConfigureAwait(false) is { Exists: true } thumbnail)
                return thumbnail;

        return null;
    }

    private async Task<FileInfo?> CreateThumbnailAsync(LocalFile localFile)
    {
        var ed2K = localFile.Ed2k;
        var thumbnailFileInfo = new FileInfo(FilePaths.ExtraFileData.ThumbnailPath(ed2K));
        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Local file {LocalFileId} has no import folder", localFile.Id);
            return null;
        }

        var fileInfo = new FileInfo(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("File does not exist for local file {LocalFileId} at \"{FilePath}\"", localFile.Id, fileInfo.FullName);
            return null;
        }

        if (!await _ffmpegService.HasVideoAsync(fileInfo).ConfigureAwait(false))
        {
            _logger.LogInformation("File for local file {LocalFileId} at \"{FilePath}\" has no video stream", localFile.Id,
                fileInfo.FullName);
            return null;
        }

        var duration = await _ffmpegService.GetDurationAsync(fileInfo).ConfigureAwait(false);
        if (duration is null)
        {
            _logger.LogWarning("Failed to get duration of video for local file {LocalFileId} at \"{FilePath}\"", localFile.Id, fileInfo.FullName);
            return null;
        }

        await _ffmpegService.ExtractThumbnailAsync(fileInfo, duration.Value, thumbnailFileInfo.FullName).ConfigureAwait(false);
        return fileInfo;
    }

    private string GetAnimePosterUri(string imageServer, string filename)
    {
        return new UriBuilder("https", imageServer, 443, $"images/main/{filename}").Uri.AbsoluteUri;
    }
}
