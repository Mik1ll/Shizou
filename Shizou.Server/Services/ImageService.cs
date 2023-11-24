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

    public static string GetAnimePosterPath(string imageFilename)
    {
        return Path.Combine(FilePaths.AnimePostersDir, imageFilename);
    }

    // ReSharper disable once InconsistentNaming
    public static string GetFileThumbnailPath(string ed2k)
    {
        return Path.Combine(FilePaths.ExtraFileDataSubDir(ed2k), "thumb.webp");
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
            var path = GetAnimePosterPath(name);
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

        var path = GetAnimePosterPath(filename);
        var uri = GetAnimePosterUri(imageServer, filename);
        _commandService.Dispatch(new GetImageCommandArgs(uri, path));
    }

    public async Task<string?> GetEpisodeThumbnailAsync(int episodeId)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFiles = context.LocalFiles.AsNoTracking().Include(lf => lf.ImportFolder).Where(lf =>
            lf.AniDbFile!.AniDbEpisodeFileXrefs.Any(ep => ep.AniDbEpisodeId == episodeId) || lf.ManualLinkEpisodeId == episodeId).ToList();
        foreach (var localFile in localFiles)
            if (File.Exists(GetFileThumbnailPath(localFile.Ed2k)))
                return localFile.Ed2k;
        foreach (var localFile in localFiles)
        {
            await GetFileThumbnailAsync(localFile).ConfigureAwait(false);
            if (File.Exists(GetFileThumbnailPath(localFile.Ed2k)))
                return localFile.Ed2k;
        }

        return null;
    }

    public async Task GetFileThumbnailAsync(LocalFile localFile, bool forceRefresh = false)
    {
        // ReSharper disable once InconsistentNaming
        var ed2k = localFile.Ed2k;
        var thumbnailFileInfo = new FileInfo(GetFileThumbnailPath(ed2k));
        if (thumbnailFileInfo.Exists)
        {
            if (!forceRefresh)
            {
                _logger.LogDebug("Found existing thumbnail for local file {LocalFileId}", localFile.Id);
                return;
            }

            _logger.LogInformation("Forcing thumbnail refresh on local file {LocalFileId}", localFile.Id);
        }

        if (localFile.ImportFolder is null)
        {
            _logger.LogWarning("Local file {LocalFileId} has no import folder", localFile.Id);
            return;
        }

        var fileInfo = new FileInfo(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        if (!fileInfo.Exists)
        {
            _logger.LogWarning("File does not exist for local file {LocalFileId} at \"{FilePath}\"", localFile.Id, fileInfo.FullName);
            return;
        }

        if (!await _ffmpegService.HasVideoAsync(fileInfo).ConfigureAwait(false))
        {
            _logger.LogInformation("File for local file {LocalFileId} at \"{FilePath}\" has no video stream, not creating thumbnail", localFile.Id,
                fileInfo.FullName);
            return;
        }

        var duration = await _ffmpegService.GetDurationAsync(fileInfo).ConfigureAwait(false);
        if (duration is null)
        {
            _logger.LogWarning("Failed to get duration of video for local file {LocalFileId} at \"{FilePath}\"", localFile.Id, fileInfo.FullName);
            return;
        }

        await _ffmpegService.ExtractThumbnailAsync(fileInfo, duration.Value, thumbnailFileInfo.FullName).ConfigureAwait(false);
    }

    // ReSharper disable once InconsistentNaming
    public async Task GetFileThumbnailAsync(string ed2k, bool forceRefresh = false)
    {
        using var context = _contextFactory.CreateDbContext();
        var localFile = context.LocalFiles.AsNoTracking().Include(lf => lf.ImportFolder).FirstOrDefault(lf => lf.Ed2k == ed2k);
        if (localFile is null)
        {
            _logger.LogWarning("Local file with ed2k {Ed2k} does not exist", ed2k);
            return;
        }

        await GetFileThumbnailAsync(localFile, forceRefresh).ConfigureAwait(false);
    }

    private string GetAnimePosterUri(string imageServer, string filename)
    {
        return new UriBuilder("https", imageServer, 443, $"images/main/{filename}").Uri.AbsoluteUri;
    }
}
