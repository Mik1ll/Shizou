using System.Linq;
using Microsoft.EntityFrameworkCore;
using Shizou.Entities;

namespace Shizou.Extensions
{
    public static class DtoQueryExtensions
    {
        public static IQueryable<TEntity> DtoInclude<TEntity>(this IQueryable<TEntity> query)
            where TEntity : Entity
        {
            return query switch
            {
                IQueryable<AniDbFile> q => (IQueryable<TEntity>)q
                    .Include(e => e.Subtitles)
                    .Include(e => e.Audio)
                    .Include(e => e.Video)
                    .Include(e => e.LocalFile)
                    .AsSplitQuery(),
                _ => query
            };
        }
    }
}
