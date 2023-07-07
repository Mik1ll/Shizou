﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.Commands.AniDb;

public record UpdateMyListArgs(
        bool Edit,
        MyListState MyListState,
        bool? Watched = null,
        DateTimeOffset? WatchedDate = null,
        int? Lid = null,
        int? Fid = null,
        int? Aid = null, string? EpNo = null
    )
    : CommandArgs($"{nameof(UpdateMyListCommand)}_lid={Lid}_fid={Fid}_aid={Aid}_epno={EpNo}"
                  + $"_edit={Edit}_watched={Watched}_state={MyListState}");

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
        if (await ProcessRequest())
            if (await ProcessRequest())
                await ProcessRequest();
        Completed = true;
    }

    private async Task<bool> ProcessRequest()
    {
        var request = CommandArgs switch
        {
            { Lid: not null, Fid: not null, Aid: null, EpNo: null, Edit: true, Watched: not null } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Lid.Value, CommandArgs.Watched, CommandArgs.WatchedDate, CommandArgs.MyListState),
            { Fid: not null, Aid: null, EpNo: null, Edit: false, Watched: not null } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Fid.Value, CommandArgs.Edit, CommandArgs.Watched, CommandArgs.WatchedDate,
                    CommandArgs.MyListState),
            { Aid: not null, EpNo: not null, Lid: null, Fid: null } =>
                _udpRequestFactory.MyListAddRequest(CommandArgs.Aid.Value, CommandArgs.EpNo, CommandArgs.Edit, CommandArgs.Watched,
                    CommandArgs.WatchedDate, CommandArgs.MyListState),
            _ => throw new ArgumentException($"{nameof(UpdateMyListArgs)} not valid")
        };
        await request.Process();
        var retry = false;
        switch (request.ResponseCode)
        {
            case AniDbResponseCode.MyListAdded:
                break;
            case AniDbResponseCode.FileInMyList:
                if (CommandArgs is { Fid: not null })
                {
                    CommandArgs = CommandArgs with { Lid = request.ExistingEntryResult!.ListId, Edit = true };
                    retry = true;
                    _logger.LogInformation("Mylist entry already exists, retrying with edit");
                }
                break;
            case AniDbResponseCode.MultipleMyListEntries:
                break;
            case AniDbResponseCode.MyListEdited:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                if (CommandArgs is { Lid: not null })
                {
                    CommandArgs = CommandArgs with { Lid = null, Edit = false };
                    retry = true;
                    _logger.LogInformation("Mylist entry not found, retrying with add");
                }
                break;
        }
        return retry;
    }
}
