using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shizou.Server.AniDbApi.Requests.Http.Interfaces;

public interface IHttpRequest
{
    Dictionary<string, string?> Args { get; }
    bool ParametersSet { get; set; }
    string? ResponseText { get; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    Task Process();
}