using AutoMapper;
using Shizou.Dtos;
using ShizouData.Models;

namespace Shizou.MapperProfiles;

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
    }
}
