namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IPingRequest : IAniDbUdpRequest
{
    void SetParameters();
}