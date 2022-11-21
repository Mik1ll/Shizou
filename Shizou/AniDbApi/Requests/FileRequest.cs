using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public sealed class FileRequest : AniDbUdpRequest
    {
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
            MyListOther = 1 << 1
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

            TitleRomaji = 1 << 23,
            TitleKanji = 1 << 22,
            TitleEnglish = 1 << 21,
            TitlesOther = 1 << 20,
            TitlesShort = 1 << 19,
            TitlesSynonym = 1 << 18,
            // Unused = 1 << 17,
            // Unused = 1 << 16,

            EpisodeNumber = 1 << 15,
            EpisodeTitleEnglish = 1 << 14,
            EpisodeTitleRomaji = 1 << 13,
            EpisodeTitleKanji = 1 << 12,
            EpisodeRating = 1 << 11,
            EpisodeVoteCount = 1 << 10,
            // Unused = 1 << 9,
            // Unused - 1 << 8,

            GroupName = 1 << 7,
            GroupNameShort = 1 << 6,
            // Unused = 1 << 5,
            // Unused = 1 << 4,
            // Unused = 1 << 3,
            // Unused = 1 << 2,
            // Unused = 1 << 1,
            DateAnimeRecordUpdated = 1 << 0
        }

        public sealed record AniDbFileResult
        {
            public AniDbFileResult()
            {
            }

            public AniDbFileResult(string responseText, FMask fMask, AMask aMask)
            {
                var dataArr = responseText.TrimEnd().Split('|');
                var dataIdx = 0;
                FileId = int.Parse(dataArr[dataIdx++]);
                foreach (var value in Enum.GetValues<FMask>().OrderByDescending(v => v))
                    if (fMask.HasFlag(value))
                    {
                        var data = dataArr[dataIdx++];
                        if (string.IsNullOrWhiteSpace(data))
                            continue;
                        switch (value)
                        {
                            case FMask.AnimeId:
                                AnimeId = int.Parse(data);
                                break;
                            case FMask.EpisodeId:
                                EpisodeId = int.Parse(data);
                                break;
                            case FMask.GroupId:
                                GroupId = int.Parse(data);
                                break;
                            case FMask.MyListId:
                                MyListId = data != "0" ? int.Parse(data) : null;
                                break;
                            case FMask.OtherEpisodes:
                                var otherEpisodes = data.Split('\'').Select(eps =>
                                {
                                    var splitLine = eps.Split(',');
                                    return (int.Parse(splitLine[0]), int.Parse(splitLine[1]) / 100f);
                                }).ToList();
                                OtherEpisodeIds = otherEpisodes.Select(e => e.Item1).ToList();
                                OtherEpisodePercentages = otherEpisodes.Select(e => e.Item2).ToList();
                                break;
                            case FMask.IsDeprecated:
                                IsDeprecated = int.Parse(data) != 0;
                                break;
                            case FMask.State:
                                State = Enum.Parse<FileState>(data);
                                break;

                            case FMask.Size:
                                Size = long.Parse(data);
                                break;
                            case FMask.Ed2K:
                                Ed2K = data;
                                break;
                            case FMask.Md5:
                                Md5 = data;
                                break;
                            case FMask.Sha1:
                                Sha1 = data;
                                break;
                            case FMask.Crc32:
                                Crc32 = data;
                                break;
                            case FMask.VideoColorDepth:
                                VideoColorDepth = int.Parse(data);
                                break;

                            case FMask.Quality:
                                Quality = data;
                                break;
                            case FMask.Source:
                                Source = data;
                                break;
                            case FMask.AudioCodecs:
                                AudioCodecs = data.Split('\'').Where(e => e != "none").ToList();
                                break;
                            case FMask.AudioBitRates:
                                AudioBitRates = data.Split('\'').Where(e => e != "none").Select(x => int.Parse(x)).ToList();
                                break;
                            case FMask.VideoCodec:
                                VideoCodec = data != "none" ? data : null;
                                break;
                            case FMask.VideoBitRate:
                                VideoBitRate = data != "none" ? int.Parse(data) : null;
                                break;
                            case FMask.VideoResolution:
                                VideoResolution = data != "none" ? data : null;
                                break;
                            case FMask.FileExtension:
                                FileExtension = data;
                                break;

                            case FMask.DubLanguages:
                                DubLanguages = data.Split('\'').Where(e => e != "none").ToList();
                                break;
                            case FMask.SubLangugages:
                                SubLangugages = data.Split('\'').Where(e => e != "none").ToList();
                                break;
                            case FMask.LengthInSeconds:
                                LengthInSeconds = data != "0" ? int.Parse(data) : null;
                                break;
                            case FMask.Description:
                                Description = DataUnescape(data);
                                break;
                            case FMask.EpisodeAiredDate:
                                EpisodeAiredDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime : null;
                                break;
                            case FMask.AniDbFileName:
                                AniDbFileName = data;
                                break;

                            case FMask.MyListState:
                                MyListState = Enum.Parse<MyListState>(data);
                                break;
                            case FMask.MyListFileState:
                                MyListFileState = Enum.Parse<MyListFileState>(data);
                                break;
                            case FMask.MyListViewed:
                                MyListViewed = int.Parse(data) != 0;
                                break;
                            case FMask.MyListViewDate:
                                MyListViewDate = data != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime : null;
                                break;
                            case FMask.MyListStorage:
                                MyListStorage = data;
                                break;
                            case FMask.MyListSource:
                                MyListSource = data;
                                break;
                            case FMask.MyListOther:
                                MyListOther = data;
                                break;
                        }
                    }
                foreach (var value in Enum.GetValues<AMask>().OrderByDescending(v => v))
                    if (aMask.HasFlag(value))
                    {
                        var data = dataArr[dataIdx++];
                        if (string.IsNullOrWhiteSpace(data))
                            continue;
                        switch (value)
                        {
                            case AMask.TotalEpisodes:
                                TotalEpisodes = int.Parse(data);
                                break;
                            case AMask.HighestEpisodeNumber:
                                HighestEpisodeNumber = int.Parse(data);
                                break;
                            case AMask.Year:
                                Year = data;
                                break;
                            case AMask.Type:
                                Type = Enum.Parse<AnimeType>(data.Replace(" ", string.Empty), true);
                                break;
                            case AMask.RelatedAnimeIds:
                                RelatedAnimeIds = data.Split('\'').Select(x => int.Parse(x)).ToList();
                                break;
                            case AMask.RelatedAnimeTypes:
                                RelatedAnimeTypes =
                                    data.Split('\'').Select(x => Enum.Parse<RelatedAnimeType>(x)).ToList();
                                break;
                            case AMask.Categories:
                                Categories = data.Split(',').ToList();
                                break;

                            case AMask.TitleRomaji:
                                TitleRomaji = data;
                                break;
                            case AMask.TitleKanji:
                                TitleKanji = data;
                                break;
                            case AMask.TitleEnglish:
                                TitleEnglish = data;
                                break;
                            case AMask.TitlesOther:
                                TitlesOther = data.Split('\'').ToList();
                                break;
                            case AMask.TitlesShort:
                                TitlesShort = data.Split('\'').ToList();
                                break;
                            case AMask.TitlesSynonym:
                                TitlesSynonym = data.Split('\'').ToList();
                                break;

                            case AMask.EpisodeNumber:
                                EpisodeNumber = data;
                                break;
                            case AMask.EpisodeTitleEnglish:
                                EpisodeTitleEnglish = data;
                                break;
                            case AMask.EpisodeTitleRomaji:
                                EpisodeTitleRomaji = data;
                                break;
                            case AMask.EpisodeTitleKanji:
                                EpisodeTitleKanji = data;
                                break;
                            case AMask.EpisodeRating:
                                EpisodeRating = int.Parse(data);
                                break;
                            case AMask.EpisodeVoteCount:
                                EpisodeVoteCount = int.Parse(data);
                                break;

                            case AMask.GroupName:
                                GroupName = data;
                                break;
                            case AMask.GroupNameShort:
                                GroupNameShort = data;
                                break;
                            case AMask.DateAnimeRecordUpdated:
                                DateRecordUpdated = DateTimeOffset.FromUnixTimeSeconds(long.Parse(data)).UtcDateTime;
                                break;
                        }
                    }
            }

            #region FMask

            public int FileId { get; init; }
            public int? AnimeId { get; init; }
            public int? EpisodeId { get; init; }
            public int? GroupId { get; init; }
            public int? MyListId { get; init; }
            public List<int>? OtherEpisodeIds { get; init; }
            public List<float>? OtherEpisodePercentages { get; init; }
            public bool? IsDeprecated { get; init; }
            public FileState? State { get; init; }

            public long? Size { get; init; }
            public string? Ed2K { get; init; }
            public string? Md5 { get; init; }
            public string? Sha1 { get; init; }
            public string? Crc32 { get; init; }
            public int? VideoColorDepth { get; init; }

            public string? Quality { get; init; }
            public string? Source { get; init; }
            public List<string>? AudioCodecs { get; init; }
            public List<int>? AudioBitRates { get; init; }
            public string? VideoCodec { get; init; }
            public int? VideoBitRate { get; init; }
            public string? VideoResolution { get; init; }
            public string? FileExtension { get; init; }

            public List<string>? DubLanguages { get; init; }
            public List<string>? SubLangugages { get; init; }
            public int? LengthInSeconds { get; init; }
            public string? Description { get; init; }
            public DateTime? EpisodeAiredDate { get; init; }
            public string? AniDbFileName { get; init; }

            public MyListState? MyListState { get; init; }
            public MyListFileState? MyListFileState { get; init; }
            public bool? MyListViewed { get; init; }
            public DateTime? MyListViewDate { get; init; }
            public string? MyListStorage { get; init; }
            public string? MyListSource { get; init; }
            public string? MyListOther { get; init; }

            #endregion FMask

            #region AMask

            public int? TotalEpisodes { get; init; }
            public int? HighestEpisodeNumber { get; init; }
            public string? Year { get; init; }
            public AnimeType? Type { get; init; }
            public List<int>? RelatedAnimeIds { get; init; }
            public List<RelatedAnimeType>? RelatedAnimeTypes { get; init; }
            public List<string>? Categories { get; init; }

            public string? TitleRomaji { get; init; }
            public string? TitleKanji { get; init; }
            public string? TitleEnglish { get; init; }
            public List<string>? TitlesOther { get; init; }
            public List<string>? TitlesShort { get; init; }
            public List<string>? TitlesSynonym { get; init; }

            public string? EpisodeNumber { get; init; }
            public string? EpisodeTitleEnglish { get; init; }
            public string? EpisodeTitleRomaji { get; init; }
            public string? EpisodeTitleKanji { get; init; }
            public int? EpisodeRating { get; init; }
            public int? EpisodeVoteCount { get; init; }

            public string? GroupName { get; init; }
            public string? GroupNameShort { get; init; }
            public DateTime? DateRecordUpdated { get; init; }

            #endregion AMask
        }

        public const AMask DefaultAMask = AMask.GroupName | AMask.GroupNameShort | AMask.DateAnimeRecordUpdated | AMask.TitleRomaji |
                                          AMask.EpisodeTitleEnglish |
                                          AMask.EpisodeNumber | AMask.TotalEpisodes | AMask.HighestEpisodeNumber | AMask.Type | AMask.EpisodeTitleRomaji |
                                          AMask.EpisodeTitleKanji;
        public const FMask DefaultFMask = FMask.Crc32 | FMask.Md5 | FMask.Sha1 | FMask.Size | FMask.Quality | FMask.Source | FMask.State | FMask.AnimeId |
                                          FMask.AudioCodecs | FMask.DubLanguages | FMask.Ed2K | FMask.EpisodeId | FMask.GroupId | FMask.IsDeprecated |
                                          FMask.OtherEpisodes | FMask.SubLangugages | FMask.VideoCodec | FMask.VideoResolution | FMask.AudioBitRates |
                                          FMask.EpisodeAiredDate | FMask.LengthInSeconds | FMask.MyListId | FMask.MyListOther | FMask.MyListSource |
                                          FMask.MyListState | FMask.MyListStorage | FMask.MyListViewed | FMask.MyListFileState | FMask.MyListViewDate |
                                          FMask.VideoBitRate | FMask.VideoColorDepth | FMask.AniDbFileName;
        private readonly AMask _aMask;
        private readonly FMask _fMask;

        private FileRequest(IServiceProvider provider, FMask fMask, AMask aMask) : base(provider.GetRequiredService<ILogger<FileRequest>>(),
            provider.GetRequiredService<AniDbUdp>(),
            provider.GetRequiredService<AniDbUdpProcessor>())
        {
            _fMask = fMask;
            _aMask = aMask;
            Params["fmask"] = ((ulong)fMask).ToString("X10");
            Params["amask"] = aMask.ToString("X");
        }

        public FileRequest(IServiceProvider provider, int fileId, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params["fid"] = fileId.ToString();
        }

        public FileRequest(IServiceProvider provider, long fileSize, string ed2K, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params["size"] = fileSize.ToString();
            Params["ed2k"] = ed2K;
        }

        // TODO: Test if epno can take special episode string
        public FileRequest(IServiceProvider provider, int animeId, int groupId, string episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
        {
            Params["aid"] = animeId.ToString();
            Params["gid"] = groupId.ToString();
            Params["epno"] = episodeNumber;
        }

        public AniDbFileResult? FileResult { get; private set; }
        public List<int>? MultipleFilesResult { get; private set; }

        public override string Command { get; } = "FILE";
        public override Dictionary<string, string> Params { get; } = new();

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.File:
                    if (string.IsNullOrWhiteSpace(ResponseText))
                        Errored = true;
                    else
                        FileResult = new AniDbFileResult(ResponseText, _fMask, _aMask);
                    break;
                case AniDbResponseCode.MulitipleFilesFound:
                    if (ResponseText is not null)
                        MultipleFilesResult = ResponseText.Split('|').Select(fid => int.Parse(fid)).ToList();
                    break;
                case AniDbResponseCode.NoSuchFile:
                    break;
            }
        }
    }
}
