using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shizou.CommandProcessors;
using Shizou.Enums;

namespace Shizou.AniDbApi.Requests
{
    public record AniDbEpisodeResult(int EpisodeId,
        int AnimeId,
        int? DurationMinutes,
        int Rating,
        int Votes,
        int EpisodeNumber,
        EpisodeType Type,
        string TitleEnglish,
        string TitleRomaji,
        string TitleKanji,
        DateTime? AiredDate);

    public sealed class EpisodeRequest : AniDbUdpRequest
    {
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
                    GetEpisodeResult();
                    break;
                case AniDbResponseCode.NoSuchEpisode:
                    break;
            }
        }

        private void GetEpisodeResult()
        {
            if (string.IsNullOrWhiteSpace(ResponseText))
            {
                Errored = true;
                return;
            }
            var dataArr = ResponseText.Split('|');
            EpisodeResult = new AniDbEpisodeResult(int.Parse(dataArr[0]),
                int.Parse(dataArr[1]),
                dataArr[1] != "0" ? int.Parse(dataArr[2]) : null,
                int.Parse(dataArr[3]),
                int.Parse(dataArr[4]),
                dataArr[5].ParseEpisode().number,
                dataArr[5].ParseEpisode().type,
                dataArr[6],
                dataArr[7],
                dataArr[8],
                dataArr[9] != "0" ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(dataArr[9])).UtcDateTime : null);
        }
    }
}
