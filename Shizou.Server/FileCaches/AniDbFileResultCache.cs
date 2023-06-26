using System;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Server.AniDbApi.Requests.Udp.Results;

namespace Shizou.Server.FileCaches;

public class AniDbFileResultCache : FileCacheBase<AniDbFileResult, AniDbFileResult>
{
    public AniDbFileResultCache(ILogger<AniDbFileResultCache> logger) : base(logger, FilePaths.TempFileDir, TimeSpan.FromDays(1))
    {
    }
}
