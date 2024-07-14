using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shizou.Server.Services;

public interface IAnimeTitleSearchService
{
    /// <summary>
    ///     Search for an anime by title
    /// </summary>
    /// <param name="query">Search Query</param>
    /// <param name="searchSpace">Anime IDs to search</param>
    /// <returns>A list of anime Ids and their titles, sorted by relevance</returns>
    Task<List<(int, string)>?> SearchAsync(string query, HashSet<int>? searchSpace = null);

    /// <summary>
    ///     Try to retrieve the complete list of anime titles from AniDB into the in-memory cache
    ///     If titles have been requested recently, retrieve from file cache
    /// </summary>
    Task GetTitlesAsync();

    /// <summary>
    ///     Schedule the next anime titles request after the rate limit timer expires
    /// </summary>
    void ScheduleNextUpdate();
}
