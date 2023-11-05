using System;
using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IMyListAddRequest : IAniDbUdpRequest<MyListAddResponse>
{
    void SetParameters(
        int fid, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    );

    void SetParameters(
        int lid, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    );

    void SetParameters(
        int aid, string epno, bool edit, bool? watched = null, DateTimeOffset? watchedDate = null, MyListState? state = null
    );
}
