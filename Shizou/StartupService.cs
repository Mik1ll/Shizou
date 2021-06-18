using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Database;
using Shizou.Entities;

namespace Shizou
{
    public sealed class StartupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            var cmdMgr = scope.ServiceProvider.GetRequiredService<CommandManager>();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<StartupService>>();
            var aniDbUdp = scope.ServiceProvider.GetRequiredService<AniDbUdp>();
            scope.ServiceProvider.GetRequiredService<AniDbUdpProcessor>().Paused = false;

            var test = new ShizouContext();
            test.AniDbFiles.Add(new AniDbFile
            {
                Ed2K = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                Censored = false,
                Chaptered = true,
                Crc = "3242",
                Deprecated = true,
                Duration = new TimeSpan(2, 1, 5),
                Md5 = "25232",
                Sha1 = "3423",
                Updated = DateTime.UtcNow,
                FileName = "uahowh",
                FileSize = 2342342,
                FileVersion = 3,
                WatchedStatus = false,

                AudioCodecs = new List<Codec> {new("test", 2555)},
                VideoCodecs = new List<Codec>() {new Codec("teste2", 12411)}
            });
            test.SaveChanges();
            // var req = new FileRequest(scope.ServiceProvider, 2305865,
            //     Enum.GetValues<FMask>().Aggregate((a, b) => a | b),
            //     Enum.GetValues<AMask>().Aggregate((a, b) => a | b));
            // await req.Process();
            // await Task.Delay(TimeSpan.FromSeconds(30));
            //await aniDbUdp.Logout();
        }
    }
}
