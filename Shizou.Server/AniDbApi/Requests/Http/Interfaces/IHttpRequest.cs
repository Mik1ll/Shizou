using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shizou.Server.AniDbApi.Requests.Http.Interfaces;

public interface IHttpRequest
{
    string? ResponseText { get; }

    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    Task Process();
}