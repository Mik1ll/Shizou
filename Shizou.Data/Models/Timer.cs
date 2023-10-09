using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(Type), nameof(ExtraId))]
public class Timer
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required TimerType Type { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ExtraId { get; set; }
    public required DateTime Expires { get; set; }
}
