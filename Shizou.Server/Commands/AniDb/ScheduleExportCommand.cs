using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public record ScheduleExportArgs() : CommandArgs($"{nameof(ScheduleExportCommand)}");

[Command(CommandType.ScheduleExport, CommandPriority.Normal, QueueType.AniDbUdp)]
public class ScheduleExportCommand : BaseCommand<ScheduleExportArgs>
{
    private readonly ILogger<ScheduleExportCommand> _logger;
    private readonly UdpRequestFactory _udpRequestFactory;
    private readonly CommandService _commandService;

    public ScheduleExportCommand(
        ILogger<ScheduleExportCommand> logger,
        UdpRequestFactory udpRequestFactory,
        CommandService commandService)
    {
        _logger = logger;
        _udpRequestFactory = udpRequestFactory;
        _commandService = commandService;
    }

    protected override async Task ProcessInner()
    {
        var request = _udpRequestFactory.MyListExportRequest();
        await request.Process();

        void SchedulePolling()
        {
            _commandService.ScheduleCommand(new ExportPollingArgs(), 24,
                DateTimeOffset.UtcNow + TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        switch (request.ResponseCode)
        {
            case AniDbResponseCode.ExportQueued:
                _logger.LogInformation("MyList export scheduled, starting polling");
                SchedulePolling();
                break;
            case AniDbResponseCode.ExportAlreadyInQueue:
                _logger.LogDebug("MyList export already in queue, polling");
                SchedulePolling();
                break;
            case AniDbResponseCode.ExportNoSuchTemplate:
                _logger.LogError("MyList export template does not exist, aborting");
                break;
            default:
                _logger.LogError("Did not receive expected response code ({ResponseCode}) from MyList export request", request.ResponseCode);
                break;
        }
        Completed = true;
    }
}
