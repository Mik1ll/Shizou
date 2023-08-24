using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class IgnoredMessage : IEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }
}
