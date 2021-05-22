using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shizou.AniDbApi
{
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
        AudioBitRateList = 1 << 20,
        AudioCodecList = 1 << 21,
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
        ListOtherEpisodes = 1ul << 34,
        MyListId = 1ul << 35,
        GroupId = 1ul << 36,
        EpisodeId = 1ul << 37,
        AnimeId = 1ul << 38,
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

        SynonymList = 1 << 18,
        ShortNameList = 1 << 19,
        OtherName = 1 << 20,
        EnglishName = 1 << 21,
        KanjiName = 1 << 22,
        RomajiName = 1 << 23,

        CategoryList = 1 << 25,
        RelatedAnimeType = 1 << 26,
        RelatedAnimeList = 1 << 27,
        Type = 1 << 28,
        Year = 1 << 29,
        HighestEpisodeNumber = 1 << 30,
        TotalEpisodes = 1u << 31
    }

    [Flags]
    public enum FileState
    {
        CrcOk = 1, 
        CrcError = 1 << 1,
        Ver2 = 1 << 2,
        Ver3 = 1 << 3,
        Ver4 = 1 << 4,
        Ver5 = 1 << 5, 
        Uncensored = 1 << 6,
        Censored = 1 << 7
    }


    public sealed class FileRequest : AniDbUdpRequest
    {
        public static string TestEnum(ulong test)
        {
            return ((FMask)test).ToString("X");
        }

        private FileRequest(IServiceProvider provider, FMask fMask, AMask aMask) : base(provider.GetRequiredService<ILogger<FileRequest>>(),
            provider.GetRequiredService<AniDbUdp>())
        {
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
        /// This command only returns first file result, try overloads with group/anime id
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="animeName"></param>
        /// <param name="groupName"></param>
        /// <param name="episodeNumber"></param>
        /// <param name="fMask"></param>
        /// <param name="aMask"></param>
        public FileRequest(IServiceProvider provider, string animeName, string groupName, int episodeNumber, FMask fMask, AMask aMask) : this(provider, fMask, aMask)
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
        public override List<(string name, string value)> Params { get; } = new() { };

        public override async Task Process()
        {
            await SendRequest();
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
    }
}
