using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shizou.Data.Enums;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class UdpRequestFactory
{
    private readonly IServiceProvider _provider;

    public UdpRequestFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public AnimeRequest AnimeRequest(int aid, AMaskAnime aMask)
    {
        var request = _provider.GetRequiredService<AnimeRequest>();
        request.AMask = aMask;
        request.Args["aid"] = aid.ToString();
        request.Args["amask"] = ((ulong)aMask).ToString("X14");
        request.ParametersSet = true;
        return request;
    }

    public AuthRequest AuthRequest()
    {
        var request = _provider.GetRequiredService<AuthRequest>();
        var optionsSnapshot = _provider.GetRequiredService<IOptionsSnapshot<ShizouOptions>>();
        var opts = optionsSnapshot.Value;
        request.Args["user"] = opts.AniDb.Username;
        request.Args["pass"] = opts.AniDb.Password;
        request.Args["protover"] = "3";
        request.Args["client"] = "shizouudp";
        request.Args["clientver"] = "1";
        request.Args["comp"] = "1";
        request.Args["enc"] = request.Encoding.BodyName;
        request.Args["mtu"] = "1400";
        request.Args["imgserver"] = "1";
        request.Args["nat"] = "1";
        request.ParametersSet = true;
        return request;
    }

    public EpisodeRequest EpisodeRequest(int episodeId)
    {
        var request = _provider.GetRequiredService<EpisodeRequest>();
        request.Args["eid"] = episodeId.ToString();
        request.ParametersSet = true;
        return request;
    }

    // TODO: Test if epno can take special episode string
    public EpisodeRequest EpisodeRequest(int animeId, string episodeNumber)
    {
        var request = _provider.GetRequiredService<EpisodeRequest>();
        request.Args["aid"] = animeId.ToString();
        request.Args["epno"] = episodeNumber;
        request.ParametersSet = true;
        return request;
    }

    private FileRequest FileRequest(FMask fMask, AMaskFile aMask)
    {
        var request = _provider.GetRequiredService<FileRequest>();
        request.FMask = fMask;
        request.AMask = aMask;
        request.Args["fmask"] = ((ulong)fMask).ToString("X10");
        request.Args["amask"] = aMask.ToString("X");
        return request;
    }

    public FileRequest FileRequest(int fileId, FMask fMask, AMaskFile aMask)
    {
        var request = FileRequest(fMask, aMask);
        request.Args["fid"] = fileId.ToString();
        request.ParametersSet = true;
        return request;
    }

    public FileRequest FileRequest(long fileSize, string ed2K, FMask fMask, AMaskFile aMask)
    {
        var request = FileRequest(fMask, aMask);
        request.Args["size"] = fileSize.ToString();
        request.Args["ed2k"] = ed2K;
        request.ParametersSet = true;
        return request;
    }

    // TODO: Test if epno can take special episode string
    public FileRequest FileRequest(int animeId, int groupId, string episodeNumber, FMask fMask, AMaskFile aMask)
    {
        var request = FileRequest(fMask, aMask);
        request.Args["aid"] = animeId.ToString();
        request.Args["gid"] = groupId.ToString();
        request.Args["epno"] = episodeNumber;
        request.ParametersSet = true;
        return request;
    }

    public GenericRequest GenericRequest(string command, Dictionary<string, string> args)
    {
        var request = _provider.GetRequiredService<GenericRequest>();
        request.Command = command;
        args.ToList().ForEach(a => request.Args.Add(a.Key, a.Value));
        request.ParametersSet = true;
        return request;
    }

    public LogoutRequest LogoutRequest()
    {
        var request = _provider.GetRequiredService<LogoutRequest>();

        request.ParametersSet = true;
        return request;
    }

    private MyListAddRequest MyListAddRequest(
        bool edit, bool? watched, DateTimeOffset? watchedDate, MyListState? state
    )
    {
        var request = _provider.GetRequiredService<MyListAddRequest>();
        request.Args["filestate"] = ((int)MyListFileState.Normal).ToString();
        request.Args["edit"] = edit ? "1" : "0";
        if (watched is not null)
            request.Args["viewed"] = watched.Value ? "1" : "0";
        if (watchedDate is not null)
            request.Args["viewdate"] = watchedDate.Value.ToUnixTimeSeconds().ToString();
        if (state is not null)
            request.Args["state"] = ((int)state).ToString();
        return request;
    }

    public MyListAddRequest MyListAddRequest(
        int fid, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        var request = MyListAddRequest(edit, watched, watchedDate, state);
        request.Args["fid"] = fid.ToString();
        request.ParametersSet = true;
        return request;
    }

    public MyListAddRequest MyListAddRequest(
        int lid, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        var request = MyListAddRequest(true, watched, watchedDate, state);
        request.Args["lid"] = lid.ToString();
        request.ParametersSet = true;
        return request;
    }

    public MyListAddRequest MyListAddRequest(
        int aid, string epno, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    )
    {
        var request = MyListAddRequest(edit, watched, watchedDate, state);
        request.Args["aid"] = aid.ToString();
        request.Args["epno"] = epno;
        request.Args["generic"] = "1";
        request.ParametersSet = true;
        return request;
    }

    public PingRequest PingRequest()
    {
        var request = _provider.GetRequiredService<PingRequest>();
        request.ParametersSet = true;
        return request;
    }

    public NotifyListRequest NotifyListRequest()
    {
        var request = _provider.GetRequiredService<NotifyListRequest>();
        request.ParametersSet = true;
        return request;
    }

    public MessageGetRequest MessageGetRequest(int id)
    {
        var request = _provider.GetRequiredService<MessageGetRequest>();
        request.Args["type"] = "M";
        request.Args["id"] = id.ToString();
        request.ParametersSet = true;
        return request;
    }

    public NotifyGetRequest NotifyGetRequest(int id)
    {
        var request = _provider.GetRequiredService<NotifyGetRequest>();
        request.Args["type"] = "N";
        request.Args["id"] = id.ToString();
        request.ParametersSet = true;
        return request;
    }
}
