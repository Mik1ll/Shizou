using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.Server.AniDbApi.Requests.Http.Results;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class AnimeRequest : HttpRequest
{
    public AnimeRequest(IServiceProvider provider, int aid) : base(provider)
    {
        Args["request"] = "anime";
        Args["aid"] = aid.ToString();
    }

    public HttpAnimeResult? AnimeResult { get; private set; }

    public override async Task Process()
    {
        Logger.LogInformation("HTTP Getting anime {Aid} from AniDb", Args["aid"]);
        await SendRequest();
        if (ResponseText is not null)
        {
            XmlSerializer serializer = new(typeof(HttpAnimeResult));
            using var strReader = new StringReader(ResponseText);
            using var xmlReader = XmlReader.Create(strReader);
            AnimeResult = serializer.Deserialize(xmlReader) as HttpAnimeResult;
        }
    }
}
