using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shizou.Options;

namespace Shizou.AniDbApi
{
    public sealed class AuthRequest : AniDbUdpRequest
    {
        private static int _failedLoginAttempts;

        private static readonly List<TimeSpan> FailedLoginPauseTimes = new()
        {
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(2)
        };

        public AuthRequest(IServiceProvider provider) : base(provider.GetRequiredService<ILogger<AuthRequest>>(), provider.GetRequiredService<AniDbUdp>())
        {
            var opts = provider.GetRequiredService<IOptionsMonitor<ShizouOptions>>().CurrentValue;
            Params.Add(("user", opts.AniDb.Username));
            Params.Add(("pass", opts.AniDb.Password));
            Params.Add(("enc", Encoding.BodyName));
        }

        public override string Command { get; } = "AUTH";

        public override List<(string name, string value)> Params { get; } = new()
        {
            ("protover", "3"),
            ("client", "shizouudp"),
            ("clientver", "1"),
            ("comp", "1"),
            ("mtu", "1400"),
            ("imgserver", "1"),
            ("nat", "1")
        };

        public override async Task Process()
        {
            await SendRequest();
            switch (ResponseCode)
            {
                case AniDbResponseCode.LoginAccepted or AniDbResponseCode.LoginAcceptedNewVersion:
                    _failedLoginAttempts = 0;
                    var split = ResponseCodeString?.Split(" ");
                    AniDbUdp.SessionKey = split?[0];
                    var ipEndpoint = split?[1];
                    AniDbUdp.ImageServerUrl = ResponseText?.Trim();
                    AniDbUdp.LoggedIn = true;
                    break;
                case AniDbResponseCode.LoginFailed:
                    Errored = true;
                    AniDbUdp.Pause("Login failed, change credentials", TimeSpan.MaxValue);
                    break;
                case AniDbResponseCode.ClientOutdated:
                    Errored = true;
                    AniDbUdp.Pause("Login failed, client outdated", TimeSpan.MaxValue);
                    break;
                case AniDbResponseCode.ClientBanned:
                    Errored = true;
                    AniDbUdp.Pause("Login failed, client banned", TimeSpan.MaxValue);
                    break;
                case null:
                    _failedLoginAttempts = Math.Min(_failedLoginAttempts + 1, FailedLoginPauseTimes.Count - 1);
                    var pauseTime = FailedLoginPauseTimes[_failedLoginAttempts];
                    AniDbUdp.Pause($"No auth response, retrying in {pauseTime}", pauseTime);
                    break;
            }
        }
    }
}
