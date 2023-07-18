using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class IgnoredMessage : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
}
