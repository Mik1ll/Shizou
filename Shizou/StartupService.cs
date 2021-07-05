using System;
using System.Collections.Generic;
using System.Linq;
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

            test.AniDbAnimes.RemoveRange(test.AniDbAnimes.Select(e => new AniDbAnime {Id = e.Id}));
            test.AniDbFiles.RemoveRange(test.AniDbFiles.Select(e => new AniDbFile {Id = e.Id}));
            test.LocalFiles.RemoveRange(test.LocalFiles.Select(e => new LocalFile {Id = e.Id}));

            test.SaveChanges();

            var ed2K = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next()));
            test.AniDbAnimes.Add(new AniDbAnime
            {
                Title = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                AniDbEpisodes = new List<AniDbEpisode>
                {
                    new()
                    {
                        AniDbFiles = new List<AniDbFile>
                        {
                            new()
                            {
                                Ed2K = ed2K,
                                FileName = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                                Source = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                                AniDbEpisodes = test.AniDbEpisodes.ToList()
                            }
                        }
                    }
                }
            });
            test.LocalFiles.Add(new LocalFile
            {
                Ed2K = ed2K,
                Crc = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                Signature = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                PathTail = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                ImportFolder = new ImportFolder
                {
                    Name = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next())),
                    Path = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next()))
                }
            });
            test.SaveChanges();


            var blah = test.LocalFiles.GetByAniDbFile(test.AniDbFiles.First());
            var blah2 = test.AniDbFiles.GetByLocalFile(test.LocalFiles.First());
            test.LocalFiles.Remove(test.LocalFiles.First());
            test.SaveChanges();

            test.LocalFiles.Add(new LocalFile
            {
                Crc = "test",
                Signature = "test",
                Ed2K = "test",
                PathTail = "test",
                ImportFolderId = 1
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
