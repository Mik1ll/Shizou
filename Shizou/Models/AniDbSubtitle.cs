using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shizou.Models;

[Owned]
public class AniDbSubtitle : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Language { get; set; } = null!;

    public int AniDbFileId { get; set; }
    public AniDbFile AniDbFile { get; set; } = null!;
}