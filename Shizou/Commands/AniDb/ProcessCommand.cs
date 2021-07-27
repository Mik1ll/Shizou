using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed record ProcessParams(int LocalFileId) : CommandParams;

    [Command(CommandType.GetFile, CommandPriority.Default, QueueType.AniDbUdp)]
    public class ProcessCommand : BaseCommand<ProcessParams>
    {
        private readonly ShizouContext _context;

        public ProcessCommand(IServiceProvider provider, ProcessParams commandParams)
            : base(provider, provider.GetRequiredService<ILogger<ProcessCommand>>(), commandParams)
        {
            CommandId = $"{nameof(ProcessCommand)}_{commandParams.LocalFileId}";
            _context = provider.GetRequiredService<ShizouContext>();
        }

        public override string CommandId { get; }

        public override async Task Process()
        {
            var localFile = _context.LocalFiles.Find(CommandParams.LocalFileId);
            if (localFile is null)
            {
                Completed = true;
                Logger.LogWarning("Unable to process local file id: {localFileId} not found, skipping", CommandParams.LocalFileId);
                return;
            }

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
            var result = fileReq.FileResult;

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
                Duration = result.LengthInSeconds!.Value,
                Source = result.Source,
                FileVersion = result.State!.Value.FileVersion(),
                Updated = DateTime.UtcNow,
                AniDbGroup = aniDbGroup,
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
                        new AniDbAudio {Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Number = i + 1}).ToList(),
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
                Subtitles = result.SubLangugages!.Select((s, i) => new AniDbSubtitle {Language = s, Number = i + 1}).ToList(),
                FileName = result.AniDbFileName!,
                LocalFile = localFile
            };
            var aniDbFile = _context.AniDbFiles.Find(result.FileId);
            if (aniDbFile is null)
                aniDbFile = _context.AniDbFiles.Add(newAniDbFile).Entity;
            else
                _context.Entry(aniDbFile).CurrentValues.SetValues(newAniDbFile);

            // Get the anime
            // TODO: Get with AnimeRequest
            var aniDbAnime = _context.AniDbAnimes.Find(result.AnimeId);
            if (aniDbAnime is null)
                aniDbAnime = _context.AniDbAnimes.Add(new AniDbAnime
                {
                    Id = result.AnimeId!.Value,
                    Title = result.TitleRomaji!,
                    EpisodeCount = result.TotalEpisodes!.Value,
                    AnimeType = result.Type!.Value,
                    RecordUpdated = result.DateAnimeRecordUpdated!.Value
                }).Entity;

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
                    Number = result.EpisodeNumber!.ParseEpisode().number,
                    AirDate = result.EpisodeAiredDate,
                    AniDbFiles = new List<AniDbFile> {aniDbFile},
                    AniDbAnime = aniDbAnime
                }).Entity;

            // Add the file relation if not found
            if (!aniDbEpisode.AniDbFiles.Any(e => e.Id == aniDbFile.Id))
                aniDbEpisode.AniDbFiles.Add(aniDbFile);

            _context.SaveChanges();

            // Get all episodes
            // TODO: Test linq to sql
            foreach (var eid in result.OtherEpisodes!.Select(e => e.episodeId).Where(eid => _context.AniDbEpisodes.Select(e => e.Id).Contains(eid)))
            {
                var episodeReq = new EpisodeRequest(Provider, eid);
                await episodeReq.Process();
                if (episodeReq.EpisodeResult is null)
                {
                    Logger.LogWarning("Could not process local file id: {localFileId}, ed2k: {localFileEd2k}, failed to get episode id: {episodeId}",
                        localFile.Id, localFile.Ed2K, eid);
                    return;
                }
                // TODO: Create episodes and anime requests
            }

            // TODO: Handle other episodes by creating additional commands
            Completed = true;
        }
    }
}
