using Shizou.Entities;

namespace Shizou.Dtos
{
    public class AniDbAudioDto : EntityDto
    {
        public int Number { get; set; }
        public string Language { get; set; } = null!;
        public string Codec { get; set; } = null!;
        public int Bitrate { get; set; }
        public int Channels { get; set; }

        public int AniDbFileId { get; set; }

        public override AniDbAudio ToEntity()
        {
            return new()
            {
                Bitrate = Bitrate,
                Channels = Channels,
                Codec = Codec,
                Id = Id,
                Language = Language,
                Number = Number,
                AniDbFileId = AniDbFileId
            };
        }
    }
}
