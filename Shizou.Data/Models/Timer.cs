using Microsoft.EntityFrameworkCore;
using Shizou.Data.Enums;

namespace Shizou.Data.Models;

[PrimaryKey(nameof(Type), nameof(ExtraId))]
public class Timer
{
    public TimerType Type { get; set; }
    public int? ExtraId { get; set; }
    public DateTime Expires { get; set; }
}
