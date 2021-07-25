using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                Logger.LogWarning("Unable to process local file id: {localFileId} Skipping", CommandParams.LocalFileId);
                return;
            }
            var fileReq = new FileRequest(Provider, localFile.FileSize, localFile.Ed2K, FileRequest.DefaultFMask, FileRequest.DefaultAMask);
            await fileReq.Process();
            if (fileReq.FileResult is null)
            {
                Completed = true;
                return;
            }
            var result = fileReq.FileResult;
            var aniDbGroup = _context.AniDbGroups.Find(result.GroupId);
            if (aniDbGroup is null)
                aniDbGroup = _context.AniDbGroups.Add(new AniDbGroup
                {
                    Id = result.GroupId!.Value,
                    Name = result.GroupName!,
                    ShortName = result.GroupNameShort!
                }).Entity;

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

            var aniDbAnime = _context.AniDbAnimes.Find(result.AnimeId);
            if (aniDbAnime is null)
                aniDbAnime = _context.AniDbAnimes.Add(new AniDbAnime
                {
                    Id = result.AnimeId!.Value,
                    Title = result.NameRomaji!,
                    EpisodeCount = result.TotalEpisodes!.Value,
                    AnimeType = result.Type!.Value,
                    RecordUpdated = result.DateAnimeRecordUpdated!.Value
                }).Entity;


            var aniDbEpisode = _context.AniDbEpisodes.Include(e => e.AniDbFiles).FirstOrDefault(e => e.Id == result.EpisodeId);
            if (aniDbEpisode is null)
                aniDbEpisode = _context.Add(new AniDbEpisode
                {
                    Id = result.EpisodeId!.Value,
                    Title = result.EpisodeName!,
                    EpisodeType = result.EpisodeNumber![0] switch
                    {
                        'C' => EpisodeType.Credits,
                        'S' => EpisodeType.Special,
                        'T' => EpisodeType.Trailer,
                        'P' => EpisodeType.Parody,
                        _ => EpisodeType.Episode
                    },
                    Number = int.Parse(char.IsNumber(result.EpisodeNumber[0]) ? result.EpisodeNumber : result.EpisodeNumber[1..]),
                    AirDate = result.EpisodeAiredDate,
                    AniDbFiles = new List<AniDbFile> {aniDbFile},
                    AniDbAnime = aniDbAnime
                }).Entity;
            // TODO: Handle other episodes by creating additional commands
            if (!aniDbEpisode.AniDbFiles.Any(e => e.Id == aniDbFile.Id))
                aniDbEpisode.AniDbFiles.Add(aniDbFile);
            _context.SaveChanges();
            Completed = true;
        }
    }
}
