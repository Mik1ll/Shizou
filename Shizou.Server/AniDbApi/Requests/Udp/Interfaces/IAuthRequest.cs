namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAuthRequest : IAniDbUdpRequest<UdpResponse>
{
    void SetParameters();
}
