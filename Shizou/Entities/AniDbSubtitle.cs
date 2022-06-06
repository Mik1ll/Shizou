namespace Shizou.Entities
{
    public class AniDbSubtitle : Entity
    {
        public int Number { get; set; }
        public string Language { get; set; } = null!;

        public int AniDbFileId { get; set; }
        public AniDbFile AniDbFile { get; set; } = null!;
    }
}
