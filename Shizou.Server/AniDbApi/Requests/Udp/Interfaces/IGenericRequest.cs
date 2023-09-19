using System.Collections.Generic;

namespace Shizou.Server.AniDbApi.Requests.Udp.Interfaces;

public interface IGenericRequest : IAniDbUdpRequest
{
    void SetParameters(string command, Dictionary<string, string> args);
}