using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shizou.AniDbApi.Requests;

public sealed class MyListDeleteRequest : AniDbUdpRequest
{
    public enum IdType
    {
        MyListId,
        FileId
    }

    public MyListDeleteRequest(IServiceProvider provider, IdType idType, int id) : base(provider)
    {
        switch (idType)
        {
            case IdType.MyListId:
                Params["lid"] = id.ToString();
                break;
            case IdType.FileId:
                Params["fid"] = id.ToString();
                break;
        }
    }

    public MyListDeleteRequest(IServiceProvider provider, int aid, string epno) : base(provider)
    {
        Params["aid"] = aid.ToString();
        Params["epno"] = epno;
    }

    public override string Command { get; } = "MYLISTDEL";
    public override Dictionary<string, string> Params { get; } = new();

    public override async Task Process()
    {
        await SendRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.MyListDeleted:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                Errored = true;
                break;
        }
    }
}
