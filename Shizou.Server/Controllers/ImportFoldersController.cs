using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Shizou.Contracts.Dtos;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.Extensions;

namespace Shizou.Server.Controllers;

public class ImportFoldersController : EntityController<ImportFolder, ImportFolderDto>
{
    public ImportFoldersController(ILogger<ImportFoldersController> logger, ShizouContext context, IMapper mapper) : base(logger, context, mapper)
    {
    }

    /// <summary>
    ///     Get import folder by path
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    [HttpGet("path/{path}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ImportFolder> GetByPath(string path)
    {
        var importFolder = Context.ImportFolders.GetByPath(path);
        if (importFolder is null)
            return NotFound();
        return Mapper.Map<ImportFolder>(importFolder);
    }
}