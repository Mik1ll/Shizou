using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.AniDbApi.Requests;
using Shizou.CommandProcessors;
using Shizou.Database;
using Shizou.Enums;
using Shizou.Models;

namespace Shizou.Commands.AniDb
{
    public sealed record ProcessParams(int LocalFileId) : CommandParams($"{nameof(ProcessCommand)}_{LocalFileId}");

    [Command(CommandType.GetFile, CommandPriority.Default, QueueType.AniDbUdp)]
    public class ProcessCommand : BaseCommand<ProcessParams>
    {
        private readonly CommandManager _cmdMgr;
        private readonly ShizouContext _context;
        private readonly string _fileCachePath;

        public ProcessCommand(IServiceProvider provider, ProcessParams commandParams)
            : base(provider, provider.GetRequiredService<ILogger<ProcessCommand>>(), commandParams)
        {
            _context = provider.GetRequiredService<ShizouContext>();
            _cmdMgr = provider.GetRequiredService<CommandManager>();
            _fileCachePath = Path.Combine(Constants.TempFilePath, CommandParams.CommandId + ".json");
        }

        public override async Task Process()
        {
            Logger.LogInformation("Processing local file id: {localfileId}", CommandParams.LocalFileId);
            var localFile = _context.LocalFiles.Find(CommandParams.LocalFileId);
            if (localFile is null)
            {
                Completed = true;
                Logger.LogWarning("Unable to process local file id: {localFileId} not found, skipping", CommandParams.LocalFileId);
                return;
            }

            var result = await GetFileResult(localFile);
            if (result is null)
                return;

            var aniDbFile = ProcessFileResult(result, localFile);

            var newAnimes = new HashSet<int>();

            AddAnimeAndEpisode(result, newAnimes, aniDbFile);

            // Get other episodes
            if (await ProcessOtherEpisodes(result, aniDbFile, localFile, newAnimes)) return;

            _cmdMgr.DispatchRange(newAnimes.Select(id => new HttpAnimeParams(id)));

            Completed = true;
            File.Delete(_fileCachePath);
        }

        private async Task<bool> ProcessOtherEpisodes(AniDbFileResult result, AniDbFile aniDbFile, LocalFile localFile, HashSet<int> newAnimes)
        {
            if (result.OtherEpisodeIds is not null)
            {
                foreach (var eid in result.OtherEpisodeIds)
                {
                    var existingEp = _context.AniDbEpisodes.Include(e => e.AniDbFiles).FirstOrDefault(e => e.Id == eid);
                    if (existingEp is not null)
                    {
                        if (!existingEp.AniDbFiles.Contains(aniDbFile))
                        {
                            existingEp.AniDbFiles.Add(aniDbFile);
                        }
                        continue;
                    }

                    var episodeReq = new EpisodeRequest(Provider, eid);
                    await episodeReq.Process();
                    if (episodeReq.EpisodeResult is null)
                    {
                        Logger.LogWarning("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, failed to get other episode id: {episodeId}",
                            localFile.Id, localFile.Ed2K, eid);
                        return true;
                    }
                    var epResult = episodeReq.EpisodeResult;
                    var newAniDbEpisode = new AniDbEpisode
                    {
                        Id = epResult.EpisodeId,
                        TitleEnglish = epResult.TitleEnglish,
                        TitleRomaji = epResult.TitleRomaji,
                        TitleKanji = epResult.TitleKanji,
                        Number = epResult.EpisodeNumber,
                        EpisodeType = epResult.Type,
                        Duration = epResult.DurationMinutes is null ? null : TimeSpan.FromMinutes(epResult.DurationMinutes.Value),
                        AirDate = epResult.AiredDate,
                        Updated = DateTime.UtcNow,
                        AniDbFiles = new List<AniDbFile>(),
                        AniDbAnimeId = epResult.AnimeId
                    };
                    using (var episodeTransaction = _context.Database.BeginTransaction())
                    {
                        // TODO: Ensure file relation is created
                        existingEp = _context.AniDbEpisodes.Include(e => e.AniDbFiles).FirstOrDefault(e => e.Id == eid);
                        if (existingEp is not null)
                        {
                            _context.Entry(existingEp).CurrentValues.SetValues(newAniDbEpisode);
                            if (!existingEp.AniDbFiles.Contains(aniDbFile))
                                existingEp.AniDbFiles.Add(aniDbFile);
                        }
                        else
                        {
                            var otherEpAnime = _context.AniDbAnimes.Select(a => new { a.Id, a.Updated }).FirstOrDefault(a => a.Id == epResult.AnimeId);
                            if (otherEpAnime?.Updated is null)
                                newAnimes.Add(epResult.AnimeId);
                            if (otherEpAnime is null)
                            {
                                var animeReq = new AnimeRequest(Provider, newAniDbEpisode.AniDbAnimeId, AnimeRequest.DefaultAMask);
                                await animeReq.Process();
                                if (animeReq.AnimeResult is null)
                                {
                                    Logger.LogWarning(
                                        "Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, failed to get other anime id: {animeId}",
                                        localFile.Id, localFile.Ed2K, epResult.AnimeId);
                                    return true;
                                }
                                var animeResult = animeReq.AnimeResult;
                                _context.AniDbAnimes.Add(new AniDbAnime
                                {
                                    Id = animeResult.AnimeId!.Value,
                                    Title = animeResult.TitleRomaji!,
                                    EpisodeCount = animeResult.TotalEpisodes!.Value,
                                    HighestEpisode = animeResult.HighestEpisodeNumber!.Value,
                                    AnimeType = animeResult.Type!.Value,
                                    AniDbUpdated = animeResult.DateRecordUpdated!.Value
                                });
                            }
                            _context.AniDbEpisodes.Add(newAniDbEpisode);
                        }
                        _context.SaveChanges();
                        episodeTransaction.Commit();
                    }
                }
                _context.SaveChanges();
            }
            return false;
        }

        private void AddAnimeAndEpisode(AniDbFileResult result, HashSet<int> newAnimes, AniDbFile aniDbFile)
        {
            using (var animeTransaction = _context.Database.BeginTransaction())
            {
                var aniDbAnime = _context.AniDbAnimes.Find(result.AnimeId);
                if (aniDbAnime is null)
                    aniDbAnime = _context.AniDbAnimes.Add(new AniDbAnime()).Entity;
                aniDbAnime.Id = result.AnimeId!.Value;
                aniDbAnime.Title = result.TitleRomaji!;
                aniDbAnime.EpisodeCount = result.TotalEpisodes!.Value;
                aniDbAnime.HighestEpisode = result.HighestEpisodeNumber!.Value;
                aniDbAnime.AnimeType = result.Type!.Value;
                aniDbAnime.AniDbUpdated = result.DateRecordUpdated!.Value;

                if (aniDbAnime.Updated is null)
                    newAnimes.Add(aniDbAnime.Id);


                // Get the episode
                var aniDbEpisode = _context.AniDbEpisodes.Include(e => e.AniDbFiles).FirstOrDefault(e => e.Id == result.EpisodeId);
                if (aniDbEpisode is null)
                    aniDbEpisode = _context.AniDbEpisodes.Add(new AniDbEpisode
                    {
                        AniDbFiles = new List<AniDbFile> { aniDbFile }
                    }).Entity;
                aniDbEpisode.Id = result.EpisodeId!.Value;
                aniDbEpisode.TitleEnglish = result.EpisodeTitleEnglish!;
                aniDbEpisode.TitleRomaji = result.EpisodeTitleRomaji;
                aniDbEpisode.TitleKanji = result.EpisodeTitleKanji;
                aniDbEpisode.Number = result.EpisodeNumber!.ParseEpisode().number;
                aniDbEpisode.EpisodeType = result.EpisodeNumber!.ParseEpisode().type;
                aniDbEpisode.AirDate = result.EpisodeAiredDate;
                aniDbEpisode.AniDbAnime = aniDbAnime;

                // Add the file relation if not found
                if (!aniDbEpisode.AniDbFiles.Contains(aniDbFile))
                    aniDbEpisode.AniDbFiles.Add(aniDbFile);

                _context.SaveChanges();
                animeTransaction.Commit();
            }
        }


        /// <summary>
        ///     Processes file result, adding file and group data to DB. Also deletes any file-episode relations to the file.
        /// </summary>
        /// <param name="result"></param>
        /// <param name="localFile"></param>
        /// <returns>AniDb file that is tracked by the context</returns>
        private AniDbFile ProcessFileResult(AniDbFileResult result, LocalFile localFile)
        {
            // Get the group
            var newGroup = new AniDbGroup
            {
                Id = result.GroupId!.Value,
                Name = result.GroupName!,
                ShortName = result.GroupNameShort!
            };
            var existingGroup = _context.AniDbGroups.Find(result.GroupId);
            if (existingGroup is null)
                _context.AniDbGroups.Add(newGroup);
            else
                _context.Entry(existingGroup).CurrentValues.SetValues(newGroup);

            // Get the file
            var newFile = new AniDbFile
            {
                Id = result.FileId,
                Ed2K = result.Ed2K!,
                Md5 = result.Md5,
                Crc = result.Crc32,
                Sha1 = result.Sha1,
                Censored = result.State!.Value.IsCensored(),
                Chaptered = result.State!.Value.HasFlag(FileState.Chaptered),
                Deprecated = result.IsDeprecated!.Value,
                FileSize = result.Size!.Value,
                Duration = result.LengthInSeconds,
                Source = result.Source,
                FileVersion = result.State!.Value.FileVersion(),
                Updated = DateTime.UtcNow,
                AniDbGroupId = result.GroupId,
                MyListEntry = result.MyListId is null
                    ? null
                    : new AniDbMyListEntry
                    {
                        Id = result.MyListId!.Value,
                        Watched = result.MyListViewed!.Value,
                        WatchedDate = result.MyListViewDate,
                        MyListState = result.MyListState!.Value,
                        MyListFileState = result.MyListFileState!.Value
                    },
                Audio = result.AudioCodecs!.Zip(result.AudioBitRates!, (codec, bitrate) => (codec, bitrate))
                    .Zip(result.DubLanguages!, (tup, lang) => (tup.codec, tup.bitrate, lang)).Select((tuple, i) =>
                        new AniDbAudio { Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Number = i + 1 }).ToList(),
                Video = result.VideoCodec is null
                    ? null
                    : new AniDbVideo
                    {
                        Codec = result.VideoCodec,
                        BitRate = result.VideoBitRate!.Value,
                        ColorDepth = result.VideoColorDepth ?? 8,
                        Height = int.Parse(result.VideoResolution!.Split('x')[1]),
                        Width = int.Parse(result.VideoResolution!.Split('x')[0])
                    },
                Subtitles = result.SubLangugages!.Select((s, i) => new AniDbSubtitle { Language = s, Number = i + 1 }).ToList(),
                FileName = result.AniDbFileName!,
                LocalFile = localFile
            };
            var existingFile = _context.AniDbFiles
                .Include(f => f.Audio)
                .Include(f => f.Subtitles)
                .Include(f => f.AniDbEpisodes)
                .FirstOrDefault(f => f.Id == result.FileId);
            if (existingFile is null)
            {
                _context.AniDbFiles.Add(newFile);
            }
            else
            {
                _context.Entry(existingFile).CurrentValues.SetValues(newFile);
                existingFile.Audio.Clear();
                existingFile.Audio.AddRange(newFile.Audio);
                existingFile.Subtitles.Clear();
                existingFile.Subtitles.AddRange(newFile.Subtitles);
                existingFile.AniDbEpisodes.Clear();
            }
            _context.SaveChanges();
            return existingFile ?? newFile;
        }

        private async Task<AniDbFileResult?> GetFileResult(LocalFile localFile)
        {
            // Check if file was requested before and did not complete
            var result = await GetFromFileCache();
            if (result is not null)
                return result;

            var fileReq = new FileRequest(Provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
            await fileReq.Process();
            result = fileReq.FileResult;
            if (fileReq.ResponseCode == AniDbResponseCode.NoSuchFile)
            {
                Logger.LogInformation("Skipped processing local file id: {localFileId}, ed2k: {localFileEd2k}, file not found on anidb", localFile.Id,
                    localFile.Ed2K);
                Completed = true;
            }
            else if (result is null)
            {
                Logger.LogError("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, no file result", localFile.Id, localFile.Ed2K);
            }
            else
            {
                // Keep file result just in case command does not complete
                await SaveToFileCache(result);
            }
            return result;
        }

        private async Task SaveToFileCache(AniDbFileResult? result)
        {
            if (!Directory.Exists(Constants.TempFilePath))
                Directory.CreateDirectory(Constants.TempFilePath);
            using (var file = new FileStream(_fileCachePath, FileMode.Create, FileAccess.Write))
            {
                await JsonSerializer.SerializeAsync(file, result);
            }
        }

        private async Task<AniDbFileResult?> GetFromFileCache()
        {
            AniDbFileResult? result = null;
            var fileResult = new FileInfo(_fileCachePath);
            if (fileResult.Exists && fileResult.Length > 0)
                using (var file = new FileStream(_fileCachePath, FileMode.Open, FileAccess.Read))
                {
                    result = await JsonSerializer.DeserializeAsync<AniDbFileResult>(file);
                }
            return result;
        }
    }
}
