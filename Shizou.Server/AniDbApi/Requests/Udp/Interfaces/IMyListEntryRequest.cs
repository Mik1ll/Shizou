using Shizou.Data.Enums;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IMyListEntryRequest : IAniDbUdpRequest
{
    MyListEntryResult? MyListEntryResult { get; }
    void SetParameters(int aid, string epno);
    void SetParameters(int id, IdTypeFileMyList idType);
}