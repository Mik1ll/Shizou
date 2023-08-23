using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class MalAnime : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public MalStatus? Status { get; set; }
}