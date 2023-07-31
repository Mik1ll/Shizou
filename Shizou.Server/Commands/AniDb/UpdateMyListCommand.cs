using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.Commands.AniDb;

public record UpdateMyListArgs(
        bool Edit,
        MyListState? MyListState = null,
        bool? Watched = null,
        DateTimeOffset? WatchedDate = null,
        int? Lid = null,
        int? Fid = null,
        int? Aid = null, string? EpNo = null
    )
    : CommandArgs($"{nameof(UpdateMyListCommand)}_lid={Lid}_fid={Fid}_aid={Aid}_epno={EpNo}"
                  + $"_edit={Edit}_watched={Watched}_state={MyListState}_uid={Path.GetRandomFileName()[..8]}");

[Command(CommandType.UpdateMyList, CommandPriority.Normal, QueueType.AniDbUdp)]
public class UpdateMyListCommand : BaseCommand<UpdateMyListArgs>
{
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly ShizouContext _context;
    private readonly UdpRequestFactory _udpRequestFactory;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        ShizouContext context,
        UdpRequestFactory udpRequestFactory
    )
    {
        _logger = logger;
        _context = context;
        _udpRequestFactory = udpRequestFactory;
    }

    protected override async Task ProcessInner()
    {
        var request = CommandArgs switch
        {
            { Lid: not null, Fid: null, Aid: null, EpNo: null, Edit: true } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState),
            { Fid: not null, Lid: null, Aid: null, EpNo: null, Edit: false } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Fid.Value, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate,
                    CommandArgs.MyListState),
            { Aid: not null, EpNo: not null, Lid: null, Fid: null } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Aid.Value, CommandArgs.EpNo, CommandArgs.Edit, CommandArgs.Watched,
                    CommandArgs.WatchedDate, CommandArgs.MyListState),
            _ => throw new ArgumentException($"{nameof(UpdateMyListArgs)} not valid")
        };
        await request.Process();
        switch (request.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (CommandArgs is { Aid: { } aid, EpNo: { } epno } && epno != "0" && !epno.StartsWith("-"))
                {
                    var entryRequest = _udpRequestFactory.MyListEntryRequest(aid, epno);
                    await entryRequest.Process();
                    if (entryRequest.MyListEntryResult is { } result)
                    {
                        SaveMyListResult(result);
                        // ReSharper disable once MethodHasAsyncOverload
                        if (_context.AniDbGenericFiles.Find(result.FileId) is null)
                        {
                            _logger.LogDebug("Adding new generic file {GenericId}", result.FileId);
                            _context.AniDbGenericFiles.Add(new AniDbGenericFile { Id = result.FileId, AniDbEpisodeId = result.EpisodeId });
                            // ReSharper disable once MethodHasAsyncOverload
                            _context.SaveChanges();
                        }
                    }
                }
                break;
            case AniDbResponseCode.FileInMyList:
                if (CommandArgs is { Fid: not null })
                {
                    var result = request.ExistingEntryResult!;
                    SaveMyListResult(result);
                }
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.MyListEdited:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                break;
        }
        Completed = true;
    }

    private void SaveMyListResult(MyListEntryResult result)
    {
        _logger.LogDebug("Saving mylist entry {MyListId}", result.MyListId);
        var entry = new AniDbMyListEntry
        {
            Id = result.MyListId,
            FileId = result.FileId,
            Watched = result.ViewDate is not null,
            WatchedDate = result.ViewDate?.UtcDateTime,
            MyListState = result.State,
            MyListFileState = result.FileState,
            Updated = DateTime.UtcNow
        };
        // ReSharper disable once MethodHasAsyncOverload
        var eEntry = _context.AniDbMyListEntries.Find(entry.Id);
        if (eEntry is null)
            _context.AniDbMyListEntries.Add(entry);
        else
            _context.Entry(eEntry).CurrentValues.SetValues(entry);
        // ReSharper disable once MethodHasAsyncOverload
        _context.SaveChanges();
    }
}
