using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class UserResponse : UdpResponse
{
    public UserResult? UserResult { get; init; }
}

public record UserResult(string? Username = null, int? UserId = null);

public class UserRequest : AniDbUdpRequest<UserResponse>, IUserRequest
{
    public UserRequest(ILogger<UserRequest> logger, AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("USER", logger, aniDbUdpState, rateLimiter)
    {
    }

    public void SetParameters(string username)
    {
        Args["user"] = username;
        ParametersSet = true;
    }

    public void SetParameters(int userId)
    {
        Args["uid"] = userId.ToString();
        ParametersSet = true;
    }

    protected override UserResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        UserResult? userResult = null;

        switch (responseCode)
        {
            case AniDbResponseCode.User:
                userResult = Args.ContainsKey("user") ? new UserResult(UserId: int.Parse(responseText)) : new UserResult(responseText);
                break;
            case AniDbResponseCode.NoSuchUser:
                break;
        }

        return new UserResponse()
        {
            ResponseCode = responseCode,
            ResponseText = responseText,
            ResponseCodeText = responseCodeText,
            UserResult = userResult
        };
    }
}
