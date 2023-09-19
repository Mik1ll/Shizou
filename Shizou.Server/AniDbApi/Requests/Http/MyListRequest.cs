using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class MyListRequest : HttpRequest, IMyListRequest
{
    public MyListRequest(
        ILogger<MyListRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory,
        HttpRateLimiter rateLimiter
    ) : base(logger, optionsSnapshot, httpState, httpClientFactory, rateLimiter)
    {
    }

    public MyListResult? MyListResult { get; private set; }

    public void SetParameters()
    {
        Args["request"] = "mylist";
        ParametersSet = true;
    }

    protected override Task HandleResponse()
    {
        if (ResponseText is not null)
        {
            XmlSerializer serializer = new(typeof(MyListResult));
            using var strReader = new StringReader(ResponseText);
            using var xmlReader = XmlReader.Create(strReader);
            MyListResult = serializer.Deserialize(xmlReader) as MyListResult;
        }

        return Task.CompletedTask;
    }
}