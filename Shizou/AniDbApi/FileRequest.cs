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

        #region FMask

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

        public List<string>? DubLanguages { get; set; }
        public List<string>? SubLangugages { get; set; }
        public int? LengthInSeconds { get; set; }
        public string? Description { get; set; }
        public DateTime? EpisodeAiredDate { get; set; }
        public string? AniDbFileName { get; set; }

        public MyListState? MyListState { get; set; }
        public MyListFileState? MyListFileState { get; set; }
        public bool? MyListViewed { get; set; }
        public DateTime? MyListViewDate { get; set; }
        public string? MyListStorage { get; set; }
        public string? MyListSource { get; set; }
        public string? MyListOther { get; set; }

        #endregion FMask

        #region AMask

        public int? TotalEpisodes { get; set; }
        public int? HighestEpisodeNumber { get; set; }
        public string? Year { get; set; }
        public AnimeType? Type { get; set; }
        public List<int>? RelatedAnimeIds { get; set; }
        public List<RelatedAnimeType>? RelatedAnimeTypes { get; set; }
        public List<string>? Categories { get; set; }

        public string? RomajiName { get; set; }
        public string? KanjiName { get; set; }
        public string? EnglishName { get; set; }
        public List<string>? OtherNames { get; set; }
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

        #endregion AMask
    }

    [Flags]
    public enum FMask : ulong
    {
        // Unused = 1ul << 39,
        AnimeId = 1ul << 38,
        EpisodeId = 1ul << 37,
        GroupId = 1ul << 36,
        MyListId = 1ul << 35,
        OtherEpisodes = 1ul << 34,
        IsDeprecated = 1ul << 33,
        State = 1ul << 32,

        Size = 1u << 31,
        Ed2K = 1 << 30,
        Md5 = 1 << 29,
        Sha1 = 1 << 28,
        Crc32 = 1 << 27,
        // Unused = 1 << 26,
        VideoColorDepth = 1 << 25,
        // Unused = 1 << 24,

        Quality = 1 << 23,
        Source = 1 << 22,
        AudioCodecs = 1 << 21,
        AudioBitRates = 1 << 20,
        VideoCodec = 1 << 19,
        VideoBitRate = 1 << 18,
        VideoResolution = 1 << 17,
        FileExtension = 1 << 16,

        DubLanguages = 1 << 15,
        SubLangugages = 1 << 14,
        LengthInSeconds = 1 << 13,
        Description = 1 << 12,
        EpisodeAiredDate = 1 << 11,
        // Unused = 1 << 10,
        // Unused = 1 << 9,
        AniDbFileName = 1 << 8,

        MyListState = 1 << 7,
        MyListFileState = 1 << 6,
        MyListViewed = 1 << 5,
        MyListViewDate = 1 << 4,
        MyListStorage = 1 << 3,
        MyListSource = 1 << 2,
        MyListOther = 1 << 1,
        // Unused = 1 << 0,
    }

    [Flags]
    public enum AMask : uint
    {
        TotalEpisodes = 1u << 31,
        HighestEpisodeNumber = 1 << 30,
        Year = 1 << 29,
        Type = 1 << 28,
        RelatedAnimeIds = 1 << 27,
        RelatedAnimeTypes = 1 << 26,
        Categories = 1 << 25,
        // Unused = 1 << 24,

        RomajiName = 1 << 23,
        KanjiName = 1 << 22,
        EnglishName = 1 << 21,
        OtherNames = 1 << 20,
        ShortNames = 1 << 19,
        Synonyms = 1 << 18,
        // Unused = 1 << 17,
        // Unused = 1 << 16,

        EpisodeNumber = 1 << 15,
        EpisodeName = 1 << 14,
        EpisodeRomajiName = 1 << 13,
        EpisodeKanjiName = 1 << 12,
        EpisodeRating = 1 << 11,
        EpisodeVoteCount = 1 << 10,
        // Unused = 1 << 9,
        // Unused - 1 << 8,

        GroupName = 1 << 7,
        GroupShortName = 1 << 6,
        // Unused = 1 << 5,
        // Unused = 1 << 4,
        // Unused = 1 << 3,
        // Unused = 1 << 2,
        // Unused = 1 << 1,
        DateAnimeRecordUpdated = 1 << 0,
    }


    public sealed class FileRequest : AniDbUdpRequest
    {
        private readonly AMask _aMask;
        private readonly FMask _fMask;
        public AniDbFileResult? Result { get; private set; }

        private FileRequest(IServiceProvider provider, FMask fMask, AMask aMask) : base(provider.GetRequiredService<ILogger<FileRequest>>(),
            provider.GetRequiredService<AniDbUdp>())
        {
            _fMask = fMask;
            _aMask = aMask;
            Params.Add(("fmask", ((ulong)fMask).ToString("X10")));
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
            Result = new AniDbFileResult(int.Parse(dataArr[dataIdx++]));
            foreach (var value in Enum.GetValues<FMask>().OrderByDescending(v => v))
                if (_fMask.HasFlag(value))
                {
                    string data = dataArr[dataIdx++];
                    if (string.IsNullOrWhiteSpace(data))
                        continue;
                    switch (value)
                    {
                        case FMask.AnimeId:
                            Result.AnimeId = int.Parse(data);
                            break;
                        case FMask.EpisodeId:
                            Result.EpisodeId = int.Parse(data);
                            break;
                        case FMask.GroupId:
                            Result.GroupId = int.Parse(data);
                            break;
                        case FMask.MyListId:
                            Result.MyListId = int.Parse(data);
                            break;
                        case FMask.OtherEpisodes:
                            Result.OtherEpisodes = data.Split('\'').Select(eps =>
                            {
                                var splitLine = eps.Split(',');
                                return (int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f);
                            }).ToList();
                            break;
                        case FMask.IsDeprecated:
                            Result.IsDeprecated = int.Parse(data) != 0;
                            break;
                        case FMask.State:
                            Result.State = Enum.Parse<FileState>(data);
                            break;

                        case FMask.Size:
                            Result.Size = long.Parse(data);
                            break;
                        case FMask.Ed2K:
                            Result.Ed2K = data;
                            break;
                        case FMask.Md5:
                            Result.Md5 = data;
                            break;
                        case FMask.Sha1:
                            Result.Sha1 = data;
                            break;
                        case FMask.Crc32:
                            Result.Crc32 = data;
                            break;
                        case FMask.VideoColorDepth:
                            Result.VideoColorDepth = int.Parse(data);
                            break;

                        case FMask.Quality:
                            Result.Quality = data;
                            break;
                        case FMask.Source:
                            Result.Source = data;
                            break;
                        case FMask.AudioCodecs:
                            Result.AudioCodecs = data.Split('\'').ToList();
                            break;
                        case FMask.AudioBitRates:
                            Result.AudioBitRates = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case FMask.VideoCodec:
                            Result.VideoCodec = data;
                            break;
                        case FMask.VideoBitRate:
                            Result.VideoBitRate = int.Parse(data);
                            break;
                        case FMask.VideoResolution:
                            Result.VideoResolution = data;
                            break;
                        case FMask.FileExtension:
                            Result.FileExtension = data;
                            break;

                        case FMask.DubLanguages:
                            Result.DubLanguages = data.Split('\'').ToList();
                            break;
                        case FMask.SubLangugages:
                            Result.SubLangugages = data.Split('\'').ToList();
                            break;
                        case FMask.LengthInSeconds:
                            Result.LengthInSeconds = int.Parse(data);
                            break;
                        case FMask.Description:
                            Result.Description = DataUnescape(data);
                            break;
                        case FMask.EpisodeAiredDate:
                            Result.EpisodeAiredDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime : null;
                            break;
                        case FMask.AniDbFileName:
                            Result.AniDbFileName = DataUnescape(data);
                            break;

                        case FMask.MyListState:
                            Result.MyListState = Enum.Parse<MyListState>(data);
                            break;
                        case FMask.MyListFileState:
                            Result.MyListFileState = Enum.Parse<MyListFileState>(data);
                            break;
                        case FMask.MyListViewed:
                            Result.MyListViewed = int.Parse(data) != 0;
                            break;
                        case FMask.MyListViewDate:
                            Result.MyListViewDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                        case FMask.MyListStorage:
                            Result.MyListStorage = data;
                            break;
                        case FMask.MyListSource:
                            Result.MyListSource = data;
                            break;
                        case FMask.MyListOther:
                            Result.MyListOther = data;
                            break;
                    }
                }
            foreach (var value in Enum.GetValues<AMask>().OrderByDescending(v => v))
                if (_aMask.HasFlag(value))
                {
                    string data = dataArr[dataIdx++];
                    if (string.IsNullOrWhiteSpace(data))
                        continue;
                    switch (value)
                    {
                        case AMask.TotalEpisodes:
                            Result.TotalEpisodes = int.Parse(data);
                            break;
                        case AMask.HighestEpisodeNumber:
                            Result.HighestEpisodeNumber = int.Parse(data);
                            break;
                        case AMask.Year:
                            Result.Year = data;
                            break;
                        case AMask.Type:
                            Result.Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                            break;
                        case AMask.RelatedAnimeIds:
                            Result.RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                            break;
                        case AMask.RelatedAnimeTypes:
                            Result.RelatedAnimeTypes = data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x.Replace(" ", string.Empty), true)).ToList();
                            break;
                        case AMask.Categories:
                            Result.Categories = data.Split(',').ToList();
                            break;

                        case AMask.RomajiName:
                            Result.RomajiName = data;
                            break;
                        case AMask.KanjiName:
                            Result.KanjiName = data;
                            break;
                        case AMask.EnglishName:
                            Result.EnglishName = data;
                            break;
                        case AMask.OtherNames:
                            Result.OtherNames = data.Split('\'').ToList();
                            break;
                        case AMask.ShortNames:
                            Result.ShortNames = data.Split('\'').ToList();
                            break;
                        case AMask.Synonyms:
                            Result.Synonyms = data.Split('\'').ToList();
                            break;

                        case AMask.EpisodeNumber:
                            Result.EpisodeNumber = data;
                            break;
                        case AMask.EpisodeName:
                            Result.EpisodeName = data;
                            break;
                        case AMask.EpisodeRomajiName:
                            Result.EpisodeRomajiName = data;
                            break;
                        case AMask.EpisodeKanjiName:
                            Result.EpisodeKanjiName = data;
                            break;
                        case AMask.EpisodeRating:
                            Result.EpisodeRating = int.Parse(data);
                            break;
                        case AMask.EpisodeVoteCount:
                            Result.EpisodeVoteCount = int.Parse(data);
                            break;

                        case AMask.GroupName:
                            Result.GroupName = data;
                            break;
                        case AMask.GroupShortName:
                            Result.GroupShortName = data;
                            break;
                        case AMask.DateAnimeRecordUpdated:
                            Result.DateAnimeRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                            break;
                    }
                }

            return Result;
        }
    }
}
