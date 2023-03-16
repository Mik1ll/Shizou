using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi.Requests.Http.Results;

namespace Shizou.AniDbApi.Requests.Http;

public class MyListRequest : HttpRequest
{
    public HttpMyListResult? MyListResult;

    public MyListRequest(IServiceProvider provider) : base(provider)
    {
        Params["request"] = "mylist";
    }

    public override async Task Process()
    {
        Logger.LogInformation("HTTP Getting mylist from AniDb");
        await SendRequest();
        if (ResponseText is not null)
        {
            XmlSerializer serializer = new(typeof(HttpMyListResult));
            MyListResult = serializer.Deserialize(XmlReader.Create(new StringReader(ResponseText))) as HttpMyListResult;
            if (MyListResult is null)
                Errored = true;
        }
    }
}
