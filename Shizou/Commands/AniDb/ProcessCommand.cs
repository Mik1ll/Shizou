using System;
using System.Linq;
using System.Threading.Tasks;
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
                return;
            var result = fileReq.FileResult;
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
                AniDbGroupId = result.GroupId,
                MyListEntry = new AniDbMyListEntry
                {
                    Watched = result.MyListViewed!.Value,
                    WatchedDate = result.MyListViewDate,
                    MyListState = result.MyListState!.Value,
                    MyListFileState = result.MyListFileState!.Value
                },
                Audio = result.AudioCodecs!.Zip(result.AudioBitRates!, (codec, bitrate) => (codec, bitrate))
                    .Zip(result.DubLanguages!, (tup, lang) => (tup.codec, tup.bitrate, lang)).Select((tuple, i) =>
                        new AniDbAudio {Bitrate = tuple.bitrate, Codec = tuple.codec, Language = tuple.lang, Number = i + 1}).ToList(),
                Video = result.VideoCodec is null
                    ? null
                    : new AniDbVideo
                    {
                        Codec = result.VideoCodec,
                        BitRate = result.VideoBitRate!.Value,
                        ColorDepth = result.VideoColorDepth!.Value,
                        Height = int.Parse(result.VideoResolution!.Split('x')[1]),
                        Width = int.Parse(result.VideoResolution!.Split('x')[0])
                    },
                Subtitles = result.SubLangugages!.Select((s, i) => new AniDbSubtitle {Language = s, Number = i + 1}).ToList(),
                FileName = result.AniDbFileName!,
                LocalFile = localFile
            };
            var aniDbFile = _context.AniDbFiles.Find(result.FileId);
            if (aniDbFile is null)
                _context.AniDbFiles.Add(newAniDbFile);
            else
                _context.Entry(localFile).CurrentValues.SetValues(newAniDbFile);
        }
    }
}
