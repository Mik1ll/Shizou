using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shizou.Server.Services;

public interface IAnimeTitleSearchService
{
    Task<List<(int, string)>?> SearchAsync(string query, HashSet<int>? searchSpace = null);
    Task GetTitlesAsync();
    void ScheduleNextUpdate();
}
