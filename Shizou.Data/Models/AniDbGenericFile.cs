using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(AniDbEpisodeId), IsUnique = true)]
public class AniDbGenericFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required int AniDbEpisodeId { get; set; }
}
