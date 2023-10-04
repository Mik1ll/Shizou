using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

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
public class UpdateMyListCommand : Command<UpdateMyListArgs>
{
    private readonly ShizouContext _context;
    private readonly ILogger<UpdateMyListCommand> _logger;
    private readonly IMyListAddRequest _myListAddRequest;
    private readonly IMyListEntryRequest _myListEntryRequest;

    public UpdateMyListCommand(
        ILogger<UpdateMyListCommand> logger,
        ShizouContext context,
        IMyListAddRequest myListAddRequest,
        IMyListEntryRequest myListEntryRequest
    )
    {
        _logger = logger;
        _context = context;
        _myListAddRequest = myListAddRequest;
        _myListEntryRequest = myListEntryRequest;
    }

    protected override async Task ProcessInner()
    {
        switch (CommandArgs)
        {
            case { Lid: not null, Fid: null, Aid: null, EpNo: null, Edit: true }:
                _myListAddRequest.SetParameters(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            case { Fid: not null, Lid: null, Aid: null, EpNo: null, Edit: false }:
                _myListAddRequest.SetParameters(CommandArgs.Fid.Value, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState);
                break;
            case { Aid: not null, EpNo: not null, Lid: null, Fid: null }:
                _myListAddRequest.SetParameters(CommandArgs.Aid.Value, CommandArgs.EpNo, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate,
                    CommandArgs.MyListState);
                break;
            default: throw new ArgumentException($"{nameof(UpdateMyListArgs)} not valid");
        }

        await _myListAddRequest.Process();
        switch (_myListAddRequest.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                if (CommandArgs is { Aid: { } aid, EpNo: { } epno } && epno != "0" && !epno.StartsWith("-"))
                {
                    _myListEntryRequest.SetParameters(aid, epno);
                    await _myListEntryRequest.Process();
                    if (_myListEntryRequest.MyListEntryResult is { } result)
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
                else if (_myListAddRequest.AddedEntryId is not null && CommandArgs.Fid is not null)
                {
                    SaveMyListId(CommandArgs.Fid.Value, _myListAddRequest.AddedEntryId.Value);
                }

                break;
            case AniDbResponseCode.FileInMyList:
                if (CommandArgs is { Fid: not null })
                    SaveMyListId(_myListAddRequest.ExistingEntryResult!.FileId, _myListAddRequest.ExistingEntryResult!.MyListId);
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
                         on ws.AniDbEpisodeId equals gf.AniDbEpisodeId
                     where gf.Id == fileId
                     select ws).FirstOrDefault() is { } epWatchedState)
            epWatchedState.MyListId = myListId;

        _context.SaveChanges();
    }
}