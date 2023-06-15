using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Owned]
public class AniDbSubtitle : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }

    public required string Language { get; set; }

    public required int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = null!;
}
