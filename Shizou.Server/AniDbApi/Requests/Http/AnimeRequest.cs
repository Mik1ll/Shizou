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

public class AnimeRequest : HttpRequest, IAnimeRequest
{
    public AnimeRequest(
        ILogger<AnimeRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory,
        HttpRateLimiter rateLimiter
    ) : base(logger, optionsSnapshot, httpState, httpClientFactory, rateLimiter)
    {
    }

    public AnimeResult? AnimeResult { get; private set; }

    public void SetParameters(int aid)
    {
        Args["request"] = "anime";
        Args["aid"] = aid.ToString();
        ParametersSet = true;
    }

    protected override Task HandleResponse()
    {
        if (ResponseText is not null)
        {
            XmlSerializer serializer = new(typeof(AnimeResult));
            using var strReader = new StringReader(ResponseText);
            using var xmlReader = XmlReader.Create(strReader);
            AnimeResult = serializer.Deserialize(xmlReader) as AnimeResult;
        }

        return Task.CompletedTask;
    }
}