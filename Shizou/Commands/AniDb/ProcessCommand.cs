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
using Shizou.Entities;
using Shizou.Enums;

namespace Shizou.Commands.AniDb
{
    public sealed record ProcessParams(int LocalFileId) : CommandParams($"{nameof(ProcessCommand)}_{LocalFileId}");

    [Command(CommandType.GetFile, CommandPriority.Default, QueueType.AniDbUdp)]
    public class ProcessCommand : BaseCommand<ProcessParams>
    {
        private readonly CommandManager _cmdMgr;
        private readonly ShizouContext _context;

        public ProcessCommand(IServiceProvider provider, ProcessParams commandParams)
            : base(provider, provider.GetRequiredService<ILogger<ProcessCommand>>(), commandParams)
        {
            _context = provider.GetRequiredService<ShizouContext>();
            _cmdMgr = provider.GetRequiredService<CommandManager>();
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

            // Check if file was requested before and did not complete
            var fileResultTempPath = Path.Combine(Program.TempFilePath, CommandParams.CommandId + ".json");
            AniDbFileResult? result = null;
            var fileResult = new FileInfo(fileResultTempPath);
            if (fileResult.Exists && fileResult.Length > 0)
                using (var file = new FileStream(fileResultTempPath, FileMode.Open, FileAccess.Read))
                {
                    result = await JsonSerializer.DeserializeAsync<AniDbFileResult>(file);
                }

            if (result is null)
            {
                var fileReq = new FileRequest(Provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
                await fileReq.Process();
                if (fileReq.ResponseCode == AniDbResponseCode.NoSuchFile)
                {
                    Logger.LogInformation("Skipped processing local file id: {localFileId}, ed2k: {localFileEd2k}, file not found on anidb", localFile.Id,
                        localFile.Ed2K);
                    Completed = true;
                    return;
                }
                if (fileReq.FileResult is null)
                {
                    Logger.LogWarning("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, no file result", localFile.Id, localFile.Ed2K);
                    return;
                }
                result = fileReq.FileResult;

                // Keep file result just in case command does not complete
                if (!Directory.Exists(Program.TempFilePath))
                    Directory.CreateDirectory(Program.TempFilePath);
                using (var file = new FileStream(fileResultTempPath, FileMode.Create, FileAccess.Write))
                {
                    await JsonSerializer.SerializeAsync(file, result);
                }
            }

            // Get the group
            var newAniDbGroup = new AniDbGroup
            {
                Id = result.GroupId!.Value,
                Name = result.GroupName!,
                ShortName = result.GroupNameShort!
            };
            var aniDbGroup = _context.AniDbGroups.Find(result.GroupId);
            if (aniDbGroup is null)
                aniDbGroup = _context.AniDbGroups.Add(newAniDbGroup).Entity;
            else
                _context.Entry(aniDbGroup).CurrentValues.SetValues(newAniDbGroup);

            // Get the file
            var newAniDbFile = new AniDbFile
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
                Duration = result.LengthInSeconds is null ? null : TimeSpan.FromSeconds(result.LengthInSeconds.Value),
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
                // TODO: Either fix Id creation for owned type or remove Id property
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
            var aniDbFile = _context.AniDbFiles.Include(f => f.Audio).Include(f => f.Subtitles).FirstOrDefault(f => f.Id == result.FileId);
            if (aniDbFile is null)
                aniDbFile = _context.AniDbFiles.Add(newAniDbFile).Entity;
            else
            {
                _context.Entry(aniDbFile).CurrentValues.SetValues(newAniDbFile);
                foreach (var audio in newAniDbFile.Audio)
                {
                    var existingAudio = aniDbFile.Audio.FirstOrDefault(a => a.Id == audio.Id);
                    if (existingAudio is null)
                        aniDbFile.Audio.Add(audio);
                    else
                        _context.Entry(existingAudio).CurrentValues.SetValues(audio);
                }
                foreach (var subtitle in newAniDbFile.Subtitles)
                {
                    var existingSubtitle = aniDbFile.Subtitles.FirstOrDefault(s => s.Id == subtitle.Id);
                    if (existingSubtitle is null)
                        aniDbFile.Subtitles.Add(subtitle);
                    else
                        _context.Entry(existingSubtitle).CurrentValues.SetValues(subtitle);
                }
            }

            var newAnime = new HashSet<int>();
            
            // Get the anime
            using (var animeTransaction = _context.Database.BeginTransaction())
            {
                var aniDbAnime = _context.AniDbAnimes.Find(result.AnimeId);
                if (aniDbAnime is null)
                    aniDbAnime = _context.AniDbAnimes.Add(new AniDbAnime
                    {
                        Id = result.AnimeId!.Value,
                        Title = result.TitleRomaji!,
                        EpisodeCount = result.TotalEpisodes!.Value,
                        HighestEpisode = result.HighestEpisodeNumber!.Value,
                        AnimeType = result.Type!.Value,
                        AniDbUpdated = result.DateAnimeRecordUpdated!.Value
                    }).Entity;

                if (aniDbAnime.Updated is null)
                    newAnime.Add(aniDbAnime.Id);

                // Get the episode
                var aniDbEpisode = _context.AniDbEpisodes.Include(e => e.AniDbFiles).FirstOrDefault(e => e.Id == result.EpisodeId);
                if (aniDbEpisode is null)
                    aniDbEpisode = _context.AniDbEpisodes.Add(new AniDbEpisode
                    {
                        Id = result.EpisodeId!.Value,
                        TitleEnglish = result.EpisodeTitleEnglish!,
                        TitleRomaji = result.EpisodeTitleRomaji,
                        TitleKanji = result.EpisodeTitleKanji,
                        Number = result.EpisodeNumber!.ParseEpisode().number,
                        EpisodeType = result.EpisodeNumber!.ParseEpisode().type,
                        AirDate = result.EpisodeAiredDate,
                        AniDbFiles = new List<AniDbFile> { aniDbFile },
                        AniDbAnime = aniDbAnime
                    }).Entity;

                // Add the file relation if not found
                if (!aniDbEpisode.AniDbFiles.Any(e => e.Id == aniDbFile.Id))
                    aniDbEpisode.AniDbFiles.Add(aniDbFile);

                _context.SaveChanges();
                animeTransaction.Commit();
            }

            // Get other episodes
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
                            _context.SaveChanges();
                        }
                        continue;
                    }

                    var episodeReq = new EpisodeRequest(Provider, eid);
                    await episodeReq.Process();
                    if (episodeReq.EpisodeResult is null)
                    {
                        Logger.LogWarning("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, failed to get other episode id: {episodeId}",
                            localFile.Id, localFile.Ed2K, eid);
                        return;
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
                                newAnime.Add(epResult.AnimeId);
                            if (otherEpAnime is null)
                                _context.AniDbAnimes.Add(new AniDbAnime
                                {
                                    Id = epResult.AnimeId,
                                    Title = "Missing Anime Info"
                                });
                            _context.AniDbEpisodes.Add(newAniDbEpisode);
                        }
                        _context.SaveChanges();
                        episodeTransaction.Commit();
                    }
                }
            }

            _cmdMgr.DispatchRange(newAnime.Select(id => new HttpAnimeParams(id)));

            Completed = true;
            File.Delete(fileResultTempPath);
        }
    }
}
