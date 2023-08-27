using System;
using System.IO;
using System.Linq;
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
    private readonly ShizouContext _context;
    private readonly ILogger<UpdateMyListCommand> _logger;
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
                        // ReSharper disable once MethodHasAsyncOverload
                        if (_context.AniDbGenericFiles.Find(result.FileId) is null)
                        {
                            _logger.LogDebug("Adding new generic file {GenericId}", result.FileId);
                            _context.AniDbGenericFiles.Add(new AniDbGenericFile { Id = result.FileId, AniDbEpisodeId = result.EpisodeId });
                            // ReSharper disable once MethodHasAsyncOverload
                            _context.SaveChanges();
                        }

                        SaveMyListId(result.FileId, result.MyListId);
                    }
                }
                else if (request.AddedEntryId is not null && CommandArgs.Fid is not null)
                {
                    SaveMyListId(request.AddedEntryId.Value, CommandArgs.Fid.Value);
                }

                break;
            case AniDbResponseCode.FileInMyList:
                if (CommandArgs is { Fid: not null })
                    SaveMyListId(request.ExistingEntryResult!.FileId, request.ExistingEntryResult!.MyListId);
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

    private void SaveMyListId(int fileId, int myListId)
    {
        if (_context.FileWatchedStates.Find(fileId) is { } fileWatchedState)
            fileWatchedState.MyListId = myListId;
        else if ((from ws in _context.EpisodeWatchedStates
                     join gf in _context.AniDbGenericFiles
                         on ws.Id equals gf.AniDbEpisodeId
                     where gf.Id == fileId
                     select ws).FirstOrDefault() is { } epWatchedState)
            epWatchedState.MyListId = myListId;

        _context.SaveChanges();
    }
}