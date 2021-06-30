namespace Shizou.Entities
{
    public class AniDbAudio : Entity
    {
        public int Number { get; set; }
        public string Language { get; set; } = null!;
        public string Codec { get; set; } = null!;
        public int Bitrate { get; set; }
        public int Channels { get; set; }

        public int AniDbFileId { get; set; }
        public virtual AniDbFile AniDbFile { get; set; } = null!;
    }
}
