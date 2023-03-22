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
                Args["lid"] = id.ToString();
                break;
            case IdType.FileId:
                Args["fid"] = id.ToString();
                break;
        }
    }

    public MyListDeleteRequest(IServiceProvider provider, int aid, string epno) : this(provider)
    {
        Args["aid"] = aid.ToString();
        Args["epno"] = epno;
    }

    public override async Task Process()
    {
        await HandleRequest();
        switch (ResponseCode)
        {
            case AniDbResponseCode.MyListDeleted:
                break;
            case AniDbResponseCode.NoSuchMyListEntry:
                break;
        }
    }
}
