using AutoMapper;
using Microsoft.Extensions.Logging;
using Shizou.Dtos;
using ShizouData.Database;
using ShizouData.Models;

namespace Shizou.Controllers;

public class LocalFilesController : EntityController<LocalFile, LocalFileDto>
{
    public LocalFilesController(ILogger<LocalFilesController> logger, ShizouContext context, IMapper mapper) : base(logger, context,
        mapper)
    {
    }
}
