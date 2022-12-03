using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Database;
using Shizou.Dtos;
using Shizou.Models;

namespace Shizou.Controllers;

public class LocalFilesController : EntityController<LocalFile, LocalFileDto>
{
    public LocalFilesController(ILogger<EntityController<LocalFile, LocalFileDto>> logger, ShizouContext context, IMapper mapper) : base(logger, context,
        mapper)
    {
    }
}
