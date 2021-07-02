using System;
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
using Shizou.Enums;
using Shizou.Import;

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
            scope.ServiceProvider.GetRequiredService<AniDbUdpProcessor>().Paused = true;
            scope.ServiceProvider.GetRequiredService<HashProcessor>().Paused = false;
            var importer = scope.ServiceProvider.GetRequiredService<Importer>();

            var test = scope.ServiceProvider.GetRequiredService<ShizouContext>();

            #region AddTest

            test.AniDbFiles.Add(new AniDbFile
            {
                Ed2K = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                Censored = false,
                Chaptered = true,
                Crc = "3242",
                Deprecated = true,
                Duration = 51,
                Md5 = "25232",
                Sha1 = "3423",
                Updated = DateTime.UtcNow,
                FileName = "uahowh",
                FileSize = 2342342,
                FileVersion = 3,
                Source = "etset",
                Watched = true,
                ReleaseDate = DateTime.Now,
                WatchedDate = DateTime.Now,
                AniDbGroup = null,
                MyListId = 922,
                MyListState = MyListState.Deleted,
                AniDbGroupId = null,
                MyListFileState = MyListFileState.Normal
            });
            test.SaveChanges();

            #endregion

            #region FileRequest

            // var req = new FileRequest(scope.ServiceProvider, 2633373,
            //     Enum.GetValues<FMask>().Aggregate((a, b) => a | b),
            //     Enum.GetValues<AMask>().Aggregate((a, b) => a | b));
            // await req.Process();
            // await Task.Delay(TimeSpan.FromSeconds(30));
            // await aniDbUdp.Logout();

            #endregion

            //importer.ScanImportFolder(1);
        }
    }
}
