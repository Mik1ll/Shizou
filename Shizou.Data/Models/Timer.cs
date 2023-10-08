using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(Type), nameof(ExtraId))]
public class Timer
{
    public required TimerType Type { get; set; }
    public int? ExtraId { get; set; }
    public required DateTime Expires { get; set; }
}
