using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Shizou.Server.Exceptions;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IAniDbUdpRequest
{
    string Command { get; set; }
    Dictionary<string, string> Args { get; }
    bool ParametersSet { get; set; }
    Encoding Encoding { get; }
    string? ResponseText { get; }
    AniDbResponseCode? ResponseCode { get; }
    string? ResponseCodeString { get; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="AniDbUdpRequestException"></exception>
    Task Process();
}