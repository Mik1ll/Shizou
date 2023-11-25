using System.ComponentModel.DataAnnotations;
using Shizou.Data.FilterCriteria;

namespace Shizou.Data.Models;

public class AnimeFilter
{
    public int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    public required AnimeCriterion Criteria { get; set; }
}
