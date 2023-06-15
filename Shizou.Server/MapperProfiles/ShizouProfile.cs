using AutoMapper;
using Shizou.Contracts.Dtos;
using Shizou.Data.Models;

namespace Shizou.Server.MapperProfiles;

public class ShizouProfile : Profile
{
    public ShizouProfile()
    {
        CreateMap<AniDbAnime, AniDbAnimeDto>().ReverseMap();
        CreateMap<AniDbAudio, AniDbAudioDto>().ReverseMap();
        CreateMap<AniDbEpisode, AniDbEpisodeDto>().ReverseMap();
        CreateMap<AniDbFile, AniDbFileDto>().ReverseMap();
        CreateMap<AniDbGroup, AniDbGroupDto>().ReverseMap();
        CreateMap<AniDbSubtitle, AniDbSubtitleDto>().ReverseMap();
        CreateMap<ImportFolder, ImportFolderDto>().ReverseMap();
        CreateMap<LocalFile, LocalFileDto>().ReverseMap();
        CreateMap<AniDbMyListEntry, AniDbMyListEntryDto>().ReverseMap();
        CreateMap<AniDbVideo, AniDbVideoDto>().ReverseMap();
    }
}
