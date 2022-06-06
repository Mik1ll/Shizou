using AutoMapper;
using Shizou.Dtos;
using Shizou.Entities;

namespace Shizou.MapperProfiles;

public class ShizouProfile : Profile
{
    public ShizouProfile()
    {
        CreateMap<AniDbAnime, AniDbAnimeDto>();
        CreateMap<AniDbAudio, AniDbAudioDto>();
        CreateMap<AniDbEpisode, AniDbEpisodeDto>();
        CreateMap<AniDbFile, AniDbFileDto>();
        CreateMap<AniDbGroup, AniDbGroupDto>();
        CreateMap<AniDbSubtitle, AniDbSubtitleDto>();
        CreateMap<CommandRequest, CommandRequestDto>();
        CreateMap<ImportFolder, ImportFolderDto>();
        CreateMap<LocalFile, LocalFileDto>();
    }
}
