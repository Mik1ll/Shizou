namespace Shizou.Dtos
{
    public class AniDbGroupDto : EntityDto
    {
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }
    }
}
