using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.CommandInputArgs;
using Shizou.Data.Database;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class CreatorCommand : Command<CreatorArgs>
{
    private readonly ILogger<CreatorCommand> _logger;
    private readonly ICreatorRequest _creatorRequest;
    private readonly IShizouContext _context;
    private readonly ImageService _imageService;

    public CreatorCommand(ILogger<CreatorCommand> logger, ICreatorRequest creatorRequest, IShizouContext context, ImageService imageService)
    {
        _logger = logger;
        _creatorRequest = creatorRequest;
        _context = context;
        _imageService = imageService;
    }

    protected override async Task ProcessInnerAsync()
    {
        _logger.LogInformation("Requesting creator info for creator id {CreatorId}", CommandArgs.CreatorId);
        _creatorRequest.SetParameters(CommandArgs.CreatorId);
        var response = await _creatorRequest.ProcessAsync().ConfigureAwait(false);
        if (response?.CreatorResult is null)
        {
            Completed = true;
            return;
        }

        var result = response.CreatorResult;
        var creator = new AniDbCreator()
        {
            Id = result.CreatorId,
            Name = result.CreatorNameTranscription ?? result.CreatorNameKanji ?? "",
            Type = result.CreatorType,
            ImageFilename = result.PictureFilename
        };
        if (_context.AniDbCreators.Find(CommandArgs.CreatorId) is { } eCreator)
            _context.Entry(eCreator).CurrentValues.SetValues(creator);
        else
            _context.AniDbCreators.Add(creator);

        _context.SaveChanges();

        if (!string.IsNullOrWhiteSpace(creator.ImageFilename) && !File.Exists(FilePaths.CreatorImagePath(creator.ImageFilename)))
            _imageService.GetCreatorImage(creator.Id);

        Completed = true;
    }
}
