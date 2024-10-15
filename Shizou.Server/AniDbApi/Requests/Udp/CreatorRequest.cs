using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class CreatorResponse : UdpResponse
{
    public CreatorResult? CreatorResult { get; init; }
}

public class CreatorRequest : AniDbUdpRequest<CreatorResponse>, ICreatorRequest
{
    public CreatorRequest(ILogger<CreatorRequest> logger, AniDbUdpState udpState, UdpRateLimiter rateLimiter) : base("CREATOR", logger, udpState, rateLimiter)
    {
    }

    public void SetParameters(int creatorId)
    {
        Args["creatorid"] = creatorId.ToString();
        ParametersSet = true;
    }

    protected override CreatorResponse CreateResponse(string responseText, AniDbResponseCode responseCode, string responseCodeText)
    {
        CreatorResult? creatorResult = null;
        switch (responseCode)
        {
            case AniDbResponseCode.Creator:
                if (!string.IsNullOrWhiteSpace(responseText))
                    creatorResult = new CreatorResult(responseText);
                break;
            case AniDbResponseCode.NoSuchCreator:
                break;
        }

        return new CreatorResponse()
        {
            CreatorResult = creatorResult,
            ResponseText = responseText,
            ResponseCode = responseCode,
            ResponseCodeText = responseCodeText
        };
    }
}
