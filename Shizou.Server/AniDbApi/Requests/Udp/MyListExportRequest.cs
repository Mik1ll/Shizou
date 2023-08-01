using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.RateLimiters;

namespace Shizou.Server.AniDbApi.Requests.Udp;

public class MyListExportRequest : AniDbUdpRequest
{
    public const string TemplateName = "txt-udp-mylist";
    public const string TemplateVersion = "0.72";

    public MyListExportRequest(ILogger<MyListExportRequest> logger,
        AniDbUdpState aniDbUdpState, UdpRateLimiter rateLimiter) : base("MYLISTEXPORT", logger, aniDbUdpState, rateLimiter)
    {
    }

    protected override Task HandleResponse()
    {
        switch (ResponseCode)
        {
            case AniDbResponseCode.ExportQueued:
                break;
            case AniDbResponseCode.ExportCancelled:
                break;
            case AniDbResponseCode.ExportAlreadyInQueue:
                break;
            case AniDbResponseCode.ExportNoSuchTemplate:
                break;
            case AniDbResponseCode.ExportNotQueuedOrProcessing:
                break;
        }
        return Task.CompletedTask;
    }
}
