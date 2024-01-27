using Microsoft.EntityFrameworkCore;

namespace Shizou.Data.Models;

[Owned]
public class AniDbSubtitle
{
    public required string Language { get; set; }
}
