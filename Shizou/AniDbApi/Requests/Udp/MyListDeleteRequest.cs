using System;
using System.Threading.Tasks;

namespace Shizou.AniDbApi.Requests.Udp;

public class MyListDeleteRequest : AniDbUdpRequest
{
    public enum IdType
    {
        MyListId,
        FileId
    }

    private MyListDeleteRequest(IServiceProvider provider) : base(provider, "MYLISTDEL")
    {
    }

    public MyListDeleteRequest(IServiceProvider provider, IdType idType, int id) : this(provider)
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

    public MyListDeleteRequest(IServiceProvider provider, int aid, string epno) : this(provider)
    {
        Params["aid"] = aid.ToString();
        Params["epno"] = epno;
    }

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
