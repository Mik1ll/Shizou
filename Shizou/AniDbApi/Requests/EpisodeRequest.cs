using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public sealed class EpisodeRequest : AniDbUdpRequest
    {
        public sealed record AniDbEpisodeResult
        {
            public AniDbEpisodeResult(string responseText)
            {
                var dataArr = responseText.Split('|');
                EpisodeId = int.Parse(dataArr[0]);
                AnimeId = int.Parse(dataArr[1]);
                DurationMinutes = dataArr[1] != "0" ? int.Parse(dataArr[2]) : null;
                Rating = int.Parse(dataArr[3]);
                Votes = int.Parse(dataArr[4]);
                (EpisodeNumber, Type) = dataArr[5].ParseEpisode();
                TitleEnglish = dataArr[6];
                TitleRomaji = dataArr[7];
                TitleKanji = dataArr[8];
                AiredDate = dataArr[9] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[9])).UtcDateTime : null;
            }

            public int EpisodeId { get; }
            public int AnimeId { get; }
            public int? DurationMinutes { get; }
            public int Rating { get; }
            public int Votes { get; }
            public int EpisodeNumber { get; }
            public EpisodeType Type { get; }
            public string TitleEnglish { get; }
            public string TitleRomaji { get; }
            public string TitleKanji { get; }
            public DateTime? AiredDate { get; }
        }

        private EpisodeRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<EpisodeRequest>>(),
            provider.GetRequiredService<AniDbUdp>(),
            provider.GetRequiredService<AniDbUdpProcessor>())
        {
        }

        public EpisodeRequest(IServiceProvider provider, int episodeId) : this(provider)
        {
            Params["eid"] = episodeId.ToString();
        }

        // TODO: Test if epno can take special episode string
        public EpisodeRequest(IServiceProvider provider, int animeId, string episodeNumber) : this(provider)
        {
            Params["aid"] = animeId.ToString();
            Params["epno"] = episodeNumber;
        }

        public override string Command { get; } = "EPISODE";
        public override Dictionary<string, string> Params { get; } = new();

        public AniDbEpisodeResult? EpisodeResult { get; private set; }

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.Episode:
                    if (string.IsNullOrWhiteSpace(ResponseText))
                        Errored = true;
                    else
                        EpisodeResult = new AniDbEpisodeResult(ResponseText);
                    break;
                case AniDbResponseCode.NoSuchEpisode:
                    break;
            }
        }
    }
}
