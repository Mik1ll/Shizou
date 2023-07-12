using System;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.Exceptions;

public class AniDbUdpRequestException : Exception
{
    public AniDbResponseCode? ResponseCode { get; }

    public AniDbUdpRequestException(string message, AniDbResponseCode? responseCode = null) : base(message)
    {
        ResponseCode = responseCode;
    }
}
