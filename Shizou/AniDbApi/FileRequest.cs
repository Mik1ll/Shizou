using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.Enums;

namespace Shizou.AniDbApi
{
    public record AniDbFileResult
    {
        public AniDbFileResult(int fileId)
        {
            FileId = fileId;
        }

        public int FileId { get; init; }
        public int? AnimeId { get; set; }
        public int? EpisodeId { get; set; }
        public int? GroupId { get; set; }
        public int? MyListId { get; set; }
        public List<(int episodeId, float percentage)>? OtherEpisodes { get; set; }
        public bool? IsDeprecated { get; set; }
        public FileState? State { get; set; }

        public long? Size { get; set; }
        public string? Ed2K { get; set; }
        public string? Md5 { get; set; }
        public string? Sha1 { get; set; }
        public string? Crc32 { get; set; }
        public int? VideoColorDepth { get; set; }

        public string? Quality { get; set; }
        public string? Source { get; set; }
        public List<string>? AudioCodecs { get; set; }
        public List<int>? AudioBitRates { get; set; }
        public string? VideoCodec { get; set; }
        public int? VideoBitRate { get; set; }
        public string? VideoResolution { get; set; }
        public string? FileExtension { get; set; }

        public string? DubLanguage { get; set; }
        public string? SubLangugage { get; set; }
        public int? LengthInSeconds { get; set; }
        public string? Description { get; set; }
        public DateTime? AiredDate { get; set; }
        public string? AniDbFileName { get; set; }

        public MyListState? MyListState { get; set; }
        public MyListFileState? MyListFileState { get; set; }
        public bool? MyListViewed { get; set; }
        public DateTime? MyListViewDate { get; set; }
        public string? MyListStorage { get; set; }
        public string? MyListSource { get; set; }
        public string? MyListOther { get; set; }
    }

    public record AniDbFileAnimeResult
    {
        public int? TotalEpisodes { get; set; }
        public int? HighestEpisodeNumber { get; set; }
        public string? Year { get; set; }
        public AnimeType? Type { get; set; }
        public List<int>? RelatedAnimeIds { get; set; }
        public string? RelatedAnimeType { get; set; }
        public List<string>? CategoryList { get; set; }

        public string? RomajiName { get; set; }
        public string? KanjiName { get; set; }
        public string? EnglishName { get; set; }
        public string? OtherName { get; set; }
        public List<string>? ShortNames { get; set; }
        public List<string>? Synonyms { get; set; }

        public string? EpisodeNumber { get; set; }
        public string? EpisodeName { get; set; }
        public string? EpisodeRomajiName { get; set; }
        public string? EpisodeKanjiName { get; set; }
        public int? EpisodeRating { get; set; }
        public int? EpisodeVoteCount { get; set; }

        public string? GroupName { get; set; }
        public string? GroupShortName { get; set; }
        public DateTime? DateAnimeRecordUpdated { get; set; }
    }

    [Flags]
    public enum FMask : ulong
    {
        MyListOther = 1 << 1,
        MyListSource = 1 << 2,
        MyListStorage = 1 << 3,
        MyListViewDate = 1 << 4,
        MyListViewed = 1 << 5,
        MyListFileState = 1 << 6,
        MyListState = 1 << 7,

        AniDbFileName = 1 << 8,
        AiredDate = 1 << 11,
        Description = 1 << 12,
        LengthInSeconds = 1 << 13,
        SubLangugage = 1 << 14,
        DubLanguage = 1 << 15,

        FileExtension = 1 << 16,
        VideoResolution = 1 << 17,
        VideoBitRate = 1 << 18,
        VideoCodec = 1 << 19,
        AudioBitRates = 1 << 20,
        AudioCodecs = 1 << 21,
        Source = 1 << 22,
        Quality = 1 << 23,

        VideoColorDepth = 1 << 25,
        Crc32 = 1 << 27,
        Sha1 = 1 << 28,
        Md5 = 1 << 29,
        Ed2K = 1 << 30,
        Size = 1ul << 31,

        State = 1ul << 32,
        IsDeprecated = 1ul << 33,
        OtherEpisodes = 1ul << 34,
        MyListId = 1ul << 35,
        GroupId = 1ul << 36,
        EpisodeId = 1ul << 37,
        AnimeId = 1ul << 38
    }

    [Flags]
    public enum AMask : uint
    {
        DateAnimeRecordUpdated = 1 << 0,
        GroupShortName = 1 << 6,
        GroupName = 1 << 7,

        EpisodeVoteCount = 1 << 10,
        EpisodeRating = 1 << 11,
        EpisodeKanjiName = 1 << 12,
        EpisodeRomajiName = 1 << 13,
        EpisodeName = 1 << 14,
        EpisodeNumber = 1 << 15,

        Synonyms = 1 << 18,
        ShortNames = 1 << 19,
        OtherName = 1 << 20,
        EnglishName = 1 << 21,
        KanjiName = 1 << 22,
        RomajiName = 1 << 23,

        Categories = 1 << 25,
        RelatedAnimeType = 1 << 26,
        RelatedAnimeIds = 1 << 27,
        Type = 1 << 28,
        Year = 1 << 29,
        HighestEpisodeNumber = 1 << 30,
        TotalEpisodes = 1u << 31
    }


    public sealed class FileRequest : AniDbUdpRequest
    {
        private readonly AMask _aMask;
        private readonly FMask _fMask;

        private FileRequest(IServiceProvider provider, FMask fMask, AMask aMask) : base(provider.GetRequiredService<ILogger<FileRequest>>(),
            provider.GetRequiredService<AniDbUdp>())
        {
            _fMask = fMask;
            _aMask = aMask;
            Params.Add(("fmask", fMask.ToString("X")[6..]));
            Params.Add(("amask", aMask.ToString("X")));
        }

        public FileRequest(IServiceProvider provider, int fileId, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params.Add(("fid", fileId.ToString()));
        }

        public FileRequest(IServiceProvider provider, long fileSize, string ed2K, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params.Add(("size", fileSize.ToString()));
            Params.Add(("ed2k", ed2K));
        }

        /// <summary>
        ///     This command only returns first file result, try overloads with group/anime id
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="animeName"></param>
        /// <param name="groupName"></param>
        /// <param name="episodeNumber"></param>
        /// <param name="fMask"></param>
        /// <param name="aMask"></param>
        public FileRequest(IServiceProvider provider, string animeName, string groupName, int episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask,
            aMask)
        {
            Params.Add(("aname", animeName));
            Params.Add(("gname", groupName));
            Params.Add(("epno", episodeNumber.ToString()));
        }

        public FileRequest(IServiceProvider provider, string animeName, int groupId, int episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params.Add(("aname", animeName));
            Params.Add(("gid", groupId.ToString()));
            Params.Add(("epno", episodeNumber.ToString()));
        }

        public FileRequest(IServiceProvider provider, int animeId, string groupName, int episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params.Add(("aid", animeId.ToString()));
            Params.Add(("gname", groupName));
            Params.Add(("epno", episodeNumber.ToString()));
        }

        public FileRequest(IServiceProvider provider, int animeId, int groupId, int episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params.Add(("aid", animeId.ToString()));
            Params.Add(("gid", groupId.ToString()));
            Params.Add(("epno", episodeNumber.ToString()));
        }

        public override string Command { get; } = "FILE";
        public override List<(string name, string value)> Params { get; } = new();

        public static string TestEnum(ulong test)
        {
            return ((FMask)test).ToString("X");
        }

        public override async Task Process()
        {
            await SendRequest();
            GetFileResult();
            // TODO: Handle file responses
            switch (ResponseCode)
            {
                case AniDbResponseCode.File:
                    break;
                case AniDbResponseCode.MulitipleFilesFound:
                    break;
                case AniDbResponseCode.NoSuchFile:
                    break;
            }
        }

        private AniDbFileResult? GetFileResult()
        {
            if (ResponseText is null)
                return null;
            string[] dataArr = ResponseText.Split('|');
            var dataIdx = 0;
            var result = new AniDbFileResult(int.Parse(dataArr[dataIdx++]));
            foreach (var value in Enum.GetValues<FMask>().OrderByDescending(v => v))
                if (_fMask.HasFlag(value))
                {
                    string data = dataArr[dataIdx++];
                    switch (value)
                    {
                        case FMask.MyListOther:
                            result.MyListOther = data;
                            break;
                        case FMask.MyListSource:
                            result.MyListSource = data;
                            break;
                        case FMask.MyListStorage:
                            result.MyListStorage = data;
                            break;
                        case FMask.MyListViewDate:
                            result.MyListViewDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case FMask.MyListViewed:
                            result.MyListViewed = int.Parse(data) != 0;
                            break;
                        case FMask.MyListFileState:
                            result.MyListFileState = Enum.Parse<MyListFileState>(data);
                            break;
                        case FMask.MyListState:
                            result.MyListState = Enum.Parse<MyListState>(data);
                            break;

                        case FMask.AniDbFileName:
                            result.AniDbFileName = DataUnescape(data);
                            break;
                        case FMask.AiredDate:
                            result.AiredDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case FMask.Description:
                            result.Description = DataUnescape(data);
                            break;
                        case FMask.LengthInSeconds:
                            result.LengthInSeconds = int.Parse(data);
                            break;
                        case FMask.SubLangugage:
                            result.SubLangugage = data;
                            break;
                        case FMask.DubLanguage:
                            result.DubLanguage = data;
                            break;

                        case FMask.FileExtension:
                            result.FileExtension = data;
                            break;
                        case FMask.VideoResolution:
                            result.VideoResolution = data;
                            break;
                        case FMask.VideoBitRate:
                            result.VideoBitRate = int.Parse(data);
                            break;
                        case FMask.VideoCodec:
                            result.VideoCodec = data;
                            break;
                        case FMask.AudioBitRates:
                            result.AudioBitRates = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case FMask.AudioCodecs:
                            result.AudioCodecs = data.Split('\'').ToList();
                            break;
                        case FMask.Source:
                            result.Source = data;
                            break;
                        case FMask.Quality:
                            result.Quality = data;
                            break;

                        case FMask.VideoColorDepth:
                            result.VideoColorDepth = int.Parse(data);
                            break;
                        case FMask.Crc32:
                            result.Crc32 = data;
                            break;
                        case FMask.Sha1:
                            result.Sha1 = data;
                            break;
                        case FMask.Md5:
                            result.Md5 = data;
                            break;
                        case FMask.Ed2K:
                            result.Ed2K = data;
                            break;
                        case FMask.Size:
                            result.Size = long.Parse(data);
                            break;

                        case FMask.State:
                            result.State = Enum.Parse<FileState>(data);
                            break;
                        case FMask.IsDeprecated:
                            result.IsDeprecated = int.Parse(data) != 0;
                            break;
                        case FMask.OtherEpisodes:
                            // TODO: Test other episodes file retrieve
                            result.OtherEpisodes = new List<(int episodeId, float percentage)>();
                            var split = data.Split("<br />");
                            foreach (var line in split)
                            {
                                var splitLine = line.Split('\'');
                                result.OtherEpisodes.Add((int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f));
                            }
                            break;
                        case FMask.MyListId:
                            result.MyListId = int.Parse(data);
                            break;
                        case FMask.GroupId:
                            result.GroupId = int.Parse(data);
                            break;
                        case FMask.EpisodeId:
                            result.EpisodeId = int.Parse(data);
                            break;
                        case FMask.AnimeId:
                            result.AnimeId = int.Parse(data);
                            break;
                    }
                }
            foreach (var value in Enum.GetValues<AMask>().OrderByDescending(v => v))
                if (_aMask.HasFlag(value))
                {
                    string data = dataArr[dataIdx++];
                    switch (value)
                    {
                    }
                }

            return result;
        }
    }
}
