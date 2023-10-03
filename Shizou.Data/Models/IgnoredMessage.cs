using System.ComponentModel.DataAnnotations.Schema;

namespace Shizou.Data.Models;

public class IgnoredMessage
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Id { get; set; }
}
