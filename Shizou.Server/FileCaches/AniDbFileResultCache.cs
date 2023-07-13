using System;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.FileCaches;

public class AniDbFileResultCache : FileCacheBase<FileResult, FileResult>
{
    public AniDbFileResultCache(ILogger<AniDbFileResultCache> logger) : base(logger, FilePaths.TempFileDir, TimeSpan.FromDays(1))
    {
    }
}
