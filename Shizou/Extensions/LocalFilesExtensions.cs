﻿using System.Linq;
using Shizou.Models;

namespace Shizou.Extensions
{
    public static class LocalFilesExtensions
    {
        public static LocalFile? GetByEd2K(this IQueryable<LocalFile> query, string ed2K)
        {
            return query.FirstOrDefault(e => e.Ed2K == ed2K);
        }

        public static LocalFile? GetByAniDbFile(this IQueryable<LocalFile> query, AniDbFile aniDbFile)
        {
            return query.GetByEd2K(aniDbFile.Ed2K);
        }

        public static IQueryable<LocalFile> GetByEpisodeId(this IQueryable<LocalFile> query, int episodeId)
        {
            return query.Where(localFile => localFile.ManualLinkEpisodeId == episodeId
                                            || localFile.AniDbFile != null
                                            && localFile.AniDbFile.AniDbEpisodes.Any(e => e.Id == episodeId));
        }
    }
}
