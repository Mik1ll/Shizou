﻿using System.Linq;
using Shizou.Data.Models;

namespace Shizou.Server.Extensions.Query;

public static class LocalFilesExtensions
{
    public static IQueryable<LocalFile> Unidentified(this IQueryable<LocalFile> query)
    {
        return from lf in query
            where lf.ManualLinkEpisodeId == null && lf.AniDbFile == null
            select lf;
    }
}
