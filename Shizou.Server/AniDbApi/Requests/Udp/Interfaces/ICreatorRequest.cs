namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface ICreatorRequest : IAniDbUdpRequest<CreatorResponse>
{
    public void SetParameters(int creatorId);
}
