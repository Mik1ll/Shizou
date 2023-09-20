using System;
using System.Threading.Tasks;
using Shizou.Server.Exceptions;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAniDbUdpRequest
{
    string? ResponseText { get; }
    AniDbResponseCode? ResponseCode { get; }
    string? ResponseCodeString { get; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="AniDbUdpRequestException"></exception>
    Task Process();
}