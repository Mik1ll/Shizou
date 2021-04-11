namespace Shizou.Entities
{
    public class AniDbGroup : Entity
    {
        public int GroupId { get; set; }
        public string Name { get; set; } = null!;
        public string ShortName { get; set; } = null!;
        public string? Url { get; set; }
    }
}
