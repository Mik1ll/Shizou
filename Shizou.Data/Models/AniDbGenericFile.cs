using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Index(nameof(AniDbEpisodeId), IsUnique = true)]
public class AniDbGenericFile : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    public int AniDbEpisodeId { get; set; }
}
