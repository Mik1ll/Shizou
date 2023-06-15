using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class AniDbGroup : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public required string? Url { get; set; }

    public List<AniDbFile> AniDbFiles { get; set; } = null!;
}
