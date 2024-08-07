﻿using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.AniDbApi.Requests.Image;

namespace Shizou.Server.Commands.AniDb;

public class GetImageCommand : Command<GetImageArgs>
{
    private readonly ILogger<GetImageCommand> _logger;
    private readonly ImageRequest _imageRequest;

    public GetImageCommand(ILogger<GetImageCommand> logger, ImageRequest imageRequest)
    {
        _logger = logger;
        _imageRequest = imageRequest;
    }

    protected override async Task ProcessInnerAsync()
    {
        _imageRequest.Url = CommandArgs.Url;
        _imageRequest.SavePath = CommandArgs.SavePath;
        await _imageRequest.ProcessAsync().ConfigureAwait(false);

        var fileInfo = new FileInfo(CommandArgs.SavePath);
        if (fileInfo is { Exists: true, Length: > 4 })
            _logger.LogInformation("Got image for url \"{Url}\", saved to \"{SavePath}\"", CommandArgs.Url, CommandArgs.SavePath);
        else
            _logger.LogError("Unable to get image for url \"{Url}\"", CommandArgs.Url);
        Completed = true;
    }
}
