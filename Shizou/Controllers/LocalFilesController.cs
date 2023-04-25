using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Dtos;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Controllers;

public class LocalFilesController : EntityController<LocalFile, LocalFileDto>
{
    public LocalFilesController(ILogger<EntityController<LocalFile, LocalFileDto>> logger, ShizouContext context, IMapper mapper) : base(logger, context,
        mapper)
    {
    }
}
