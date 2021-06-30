namespace Shizou.Entities
{
    public class AniDbSubtitle : Entity
    {
        public int Number { get; set; }
        public string Format { get; set; } = null!;
        public string Language { get; set; } = null!;

        public virtual AniDbFile AniDbFile { get; set; } = null!;
    }
}
