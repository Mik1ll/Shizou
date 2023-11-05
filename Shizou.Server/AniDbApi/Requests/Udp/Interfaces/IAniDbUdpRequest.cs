using System;
using System.Threading.Tasks;
using Shizou.Server.Exceptions;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAniDbUdpRequest<TResponse>
    where TResponse : UdpResponse
{
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="AniDbUdpRequestException"></exception>
    Task<TResponse?> Process();
}
