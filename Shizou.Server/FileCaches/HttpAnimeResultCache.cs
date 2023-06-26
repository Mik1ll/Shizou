using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Server.AniDbApi.Requests.Http.Results;

namespace Shizou.Server.FileCaches;

public class HttpAnimeResultCache : FileCacheBase<string, HttpAnimeResult>
{
    private readonly XmlSerializer _serializer;

    public HttpAnimeResultCache(ILogger<HttpAnimeResultCache> logger) : base(logger, FilePaths.HttpCacheDir, TimeSpan.FromDays(1))
    {
        _serializer = new XmlSerializer(typeof(HttpAnimeResult));
    }

    protected override Task<HttpAnimeResult?> DeserializeAsync(FileStream file)
    {
        using var xmlReader = XmlReader.Create(file);
        return Task.FromResult(_serializer.Deserialize(xmlReader) as HttpAnimeResult);
    }

    protected override async Task SerializeAsync(string value, FileStream file)
    {
        await using var sw = new StreamWriter(file);
        await sw.WriteAsync(value);
    }
}
