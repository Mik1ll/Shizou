using Shizou.Data.FilterCriteria;

namespace Shizou.Data.Models;

public class AnimeFilter
{
    public int Id { get; set; }

    public required AnimeCriterion Criteria { get; set; }
}
