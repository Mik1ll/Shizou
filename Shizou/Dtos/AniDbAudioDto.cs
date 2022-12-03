namespace Shizou.Dtos;

public class AniDbAudioDto : IEntityDto
{
    public int Id { get; set; }
    public string Language { get; set; } = null!;
    public string Codec { get; set; } = null!;
    public int Bitrate { get; set; }
}
