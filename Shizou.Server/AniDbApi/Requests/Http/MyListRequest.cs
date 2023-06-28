using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Server.AniDbApi.Requests.Http.Results;
using Shizou.Server.Options;

namespace Shizou.Server.AniDbApi.Requests.Http;

public class MyListRequest : HttpRequest
{
    public HttpMyListResult? MyListResult;

    public MyListRequest(
        ILogger<MyListRequest> logger,
        IOptionsSnapshot<ShizouOptions> optionsSnapshot,
        AniDbHttpState httpState,
        IHttpClientFactory httpClientFactory
    ) : base(logger, optionsSnapshot, httpState, httpClientFactory)
    {
    }

    public override async Task Process()
    {
        Logger.LogInformation("HTTP Getting mylist from AniDb");
        await SendRequest();
        if (ResponseText is not null)
        {
            XmlSerializer serializer = new(typeof(HttpMyListResult));
            using var strReader = new StringReader(ResponseText);
            using var xmlReader = XmlReader.Create(strReader);
            MyListResult = serializer.Deserialize(xmlReader) as HttpMyListResult;
        }
    }
}
