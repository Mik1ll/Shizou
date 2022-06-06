namespace Shizou.Dtos
{
    public class AniDbSubtitleDto : EntityDto
    {
        public int Number { get; set; }
        public string Language { get; set; } = null!;
    }
}
