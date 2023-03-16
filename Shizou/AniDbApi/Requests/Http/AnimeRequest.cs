using System;
using System.IO;
using System.Threading.Tasks;
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
            XmlSerializer serializer = new(typeof(HttpAnimeResult));
            AnimeResult = serializer.Deserialize(XmlReader.Create(new StringReader(ResponseText))) as HttpAnimeResult;
            if (AnimeResult is null) Errored = true;
        }
    }
}
