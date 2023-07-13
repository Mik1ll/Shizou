using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class AnimeRequest : HttpRequest
{
    public AnimeRequest(
        ILogger<AnimeRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory
    ) : base(logger, optionsSnapshot, httpState, httpClientFactory)
    {
    }

    public AnimeResult? AnimeResult { get; private set; }

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
