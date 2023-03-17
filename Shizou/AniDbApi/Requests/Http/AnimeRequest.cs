using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Http.Results;

namespace Shizou.AniDbApi.Requests.Http;

public class AnimeRequest : HttpRequest
{
    public AnimeRequest(IServiceProvider provider, int aid) : base(provider)
    {
        Params["request"] = "anime";
        Params["aid"] = aid.ToString();
    }

    public HttpAnimeResult? AnimeResult { get; private set; }

    public override async Task Process()
    {
        Logger.LogInformation("HTTP Getting anime from AniDb");
        await SendRequest();
        if (ResponseText is not null)
        {
            ResponseText = HttpUtility.HtmlDecode(ResponseText);
            XmlSerializer serializer = new(typeof(HttpAnimeResult));
            using var strReader = new StringReader(ResponseText);
            using var xmlReader = XmlReader.Create(strReader);
            AnimeResult = serializer.Deserialize(xmlReader) as HttpAnimeResult;
            if (AnimeResult is null) Errored = true;
        }
    }
}
