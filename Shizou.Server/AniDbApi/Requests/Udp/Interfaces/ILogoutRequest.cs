namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface ILogoutRequest : IAniDbUdpRequest<UdpResponse>
{
    void SetParameters();
}
