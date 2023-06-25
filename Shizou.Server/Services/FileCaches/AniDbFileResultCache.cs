using System;
using Shizou.Data;
using Shizou.Server.AniDbApi.Requests.Udp.Results;

namespace Shizou.Server.Services.FileCaches;

public class AniDbFileResultCache : FileCacheBase<AniDbFileResult>
{
    public AniDbFileResultCache() : base(FilePaths.TempFileDir, TimeSpan.FromDays(1))
    {
    }
}
