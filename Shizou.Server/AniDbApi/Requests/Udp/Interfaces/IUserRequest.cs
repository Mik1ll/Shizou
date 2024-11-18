namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IUserRequest : IAniDbUdpRequest<UserResponse>
{
    void SetParameters(string username);
    void SetParameters(int userId);
}
