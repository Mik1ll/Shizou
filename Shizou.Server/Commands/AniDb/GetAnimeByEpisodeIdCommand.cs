using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.CommandInputArgs;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public class GetAnimeByEpisodeIdCommand : Command<GetAnimeByEpisodeIdArgs>
{
    private readonly ILogger<GetAnimeByEpisodeIdCommand> _logger;
    private readonly IEpisodeRequest _episodeRequest;
    private readonly CommandService _commandService;

    public GetAnimeByEpisodeIdCommand(ILogger<GetAnimeByEpisodeIdCommand> logger, IEpisodeRequest episodeRequest,
        CommandService commandService)
    {
        _logger = logger;
        _episodeRequest = episodeRequest;
        _commandService = commandService;
    }

    protected override async Task ProcessInnerAsync()
    {
        _logger.LogInformation("Querying anime id from episode id: {EpisodeId}", CommandArgs.EpisodeId);
        _episodeRequest.SetParameters(CommandArgs.EpisodeId);
        var res = await _episodeRequest.ProcessAsync().ConfigureAwait(false);
        if (res is not { EpisodeResult: not null })
        {
            _logger.LogWarning("Failed to get episode data for episode id: {EpisodeId}. Response Code: {ResponseCode}", CommandArgs.EpisodeId,
                res?.ResponseCode);
            Completed = true;
            return;
        }

        _logger.LogInformation("Dispatching anime request for {AnimeId}", res.EpisodeResult.AnimeId);
        _commandService.Dispatch(new AnimeArgs(res.EpisodeResult.AnimeId));
        Completed = true;
    }
}
