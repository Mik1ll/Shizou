using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Server.AniDbApi.Requests.Http;

namespace Shizou.Server.FileCaches;

public class HttpAnimeResultCache : FileCacheBase<string, AnimeResult>
{
    private readonly XmlSerializer _serializer;

    public HttpAnimeResultCache(ILogger<HttpAnimeResultCache> logger) : base(logger, FilePaths.HttpCacheDir, TimeSpan.FromDays(1))
    {
        _serializer = new XmlSerializer(typeof(AnimeResult));
    }

    protected override Task<AnimeResult?> DeserializeAsync(FileStream file)
    {
        using var xmlReader = XmlReader.Create(file);
        return Task.FromResult(_serializer.Deserialize(xmlReader) as AnimeResult);
    }

    protected override async Task SerializeAsync(string value, FileStream file)
    {
        var sw = new StreamWriter(file);
        await using var _ = sw.ConfigureAwait(false);
        await sw.WriteAsync(value).ConfigureAwait(false);
    }
}
