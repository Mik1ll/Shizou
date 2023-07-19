using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Notify;
using Shizou.Server.Services;

namespace Shizou.Server.Commands.AniDb;

public record ExportPollArgs() : CommandArgs($"{nameof(ExportPollCommand)}");

[Command(CommandType.ExportPoll, CommandPriority.High, QueueType.AniDbUdp)]
public class ExportPollCommand : BaseCommand<ExportPollArgs>
{
    private readonly ILogger<ExportPollCommand> _logger;
    private readonly UdpRequestFactory _udpRequestFactory;
    private readonly ShizouContext _context;
    private readonly CommandService _commandService;

    public ExportPollCommand(
        ILogger<ExportPollCommand> logger,
        UdpRequestFactory udpRequestFactory,
        ShizouContext context,
        CommandService commandService)
    {
        _logger = logger;
        _udpRequestFactory = udpRequestFactory;
        _context = context;
        _commandService = commandService;
    }

    protected override async Task ProcessInner()
    {
        var request = _udpRequestFactory.NotifyListRequest();
        await request.Process();
        if (request.Result is null)
        {
            _logger.LogWarning("Notify List request did not return a result, aborting polling");
            StopPolling();
            Completed = true;
            return;
        }
        var ignoredMessages = _context.IgnoredMessages.Select(m => m.Id).ToHashSet();
        var messageIds = request.Result.Where(r => r.Type == "M")
            .Select(r => r.Id)
            .Where(id => !ignoredMessages.Contains(id))
            .ToList();
        if (messageIds is [])
        {
            _logger.LogDebug("No new/non-ignored messages returned by Notify List request");
            Completed = true;
            return;
        }
        var exportMessages = new List<MessageGetResult>();
        foreach (var messageId in messageIds)
        {
            var messageRequest = _udpRequestFactory.MessageGetRequest(messageId);
            await messageRequest.Process();
            if (messageRequest.Result is null)
            {
                _logger.LogWarning("Failed to get message id: {MessageId}, aborting polling", messageId);
                StopPolling();
                Completed = true;
                return;
            }
            if (messageRequest.Result.Title.StartsWith("[EXPORT]"))
                exportMessages.Add(messageRequest.Result);
            else
            {
                _context.IgnoredMessages.Add(new IgnoredMessage { Id = messageId });
                // ReSharper disable once MethodHasAsyncOverload
                _context.SaveChanges();
            }
        }
        var latestExport = exportMessages.OrderByDescending(m => m.Date)
            .FirstOrDefault(m =>
                m.Date > DateTimeOffset.UtcNow - TimeSpan.FromHours(24) &&
                m.Body.Contains($"Template: {MyListExportRequest.TemplateName}") &&
                m.Body.Contains("Direct Download: ")
            );
        if (latestExport is not null)
        {
            var urlMatch = Regex.Match(latestExport.Body, @"Direct Download: \[url=(.*)\](.*)\[/url\]");
            var exportUrl = urlMatch.Groups[1].Value;
            var exportFileName = urlMatch.Groups[2].Value;
            using var client = new HttpClient();
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, exportUrl);
            requestMessage.Headers.Add("User-Agent", "Shizou");
            var result = await client.SendAsync(requestMessage);
            await using var resultStream = await result.Content.ReadAsStreamAsync();
            Directory.CreateDirectory(FilePaths.MyListBackupDir);
            await using var fileStream = File.Create(Path.Combine(FilePaths.MyListBackupDir, exportFileName));
            await resultStream.CopyToAsync(fileStream);
            _commandService.Dispatch(new SyncMyListFromExportArgs(exportFileName));
        }
        foreach (var exportMessage in exportMessages)
        {
            var ackRequest = _udpRequestFactory.MessageAckRequest(exportMessage.Id);
            await ackRequest.Process();
        }
        Completed = true;
    }

    private void StopPolling()
    {
        var thisCommand = _context.ScheduledCommands.FirstOrDefault(c => c.CommandId == CommandId);
        if (thisCommand is not null)
        {
            _context.ScheduledCommands.Remove(thisCommand);
            _context.SaveChanges();
        }
    }
}
