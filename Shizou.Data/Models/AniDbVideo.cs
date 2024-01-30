namespace Shizou.Data.Models;

public class AniDbVideo
{
    public required string Codec { get; set; }
    public required int BitRate { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required int ColorDepth { get; set; }
}
