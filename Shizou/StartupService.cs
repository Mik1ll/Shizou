using System;
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
using Shizou.Extensions;
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

            var context = scope.ServiceProvider.GetRequiredService<ShizouContext>();

            #region AddTest

            /*context.AniDbAnimes.RemoveRange(context.AniDbAnimes.Select(e => new AniDbAnime {Id = e.Id}));
            context.AniDbFiles.RemoveRange(context.AniDbFiles.Select(e => new AniDbFile {Id = e.Id}));
            context.LocalFiles.RemoveRange(context.LocalFiles.Select(e => new LocalFile {Id = e.Id}));

            context.SaveChanges();

            var ed2K = Convert.ToBase64String(BitConverter.GetBytes(new Random().Next()));
            context.AniDbAnimes.Add(new AniDbAnime
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
                                AniDbEpisodes = context.AniDbEpisodes.ToList()
                            }
                        }
                    }
                }
            });
            context.LocalFiles.Add(new LocalFile
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
            context.SaveChanges();


            var blah = context.LocalFiles.GetByAniDbFile(context.AniDbFiles.First());
            var blah2 = context.AniDbFiles.GetByLocalFile(context.LocalFiles.First());
            context.LocalFiles.Remove(context.LocalFiles.First());
            context.SaveChanges();

            context.LocalFiles.Add(new LocalFile
            {
                Crc = "test",
                Signature = "test",
                Ed2K = "test",
                PathTail = "test",
                ImportFolderId = 1
            });
            context.SaveChanges();*/

            #endregion

            #region FileRequest

            // var req = new FileRequest(scope.ServiceProvider, 2633373,
            //     Enum.GetValues<FMask>().Aggregate((a, b) => a | b),
            //     Enum.GetValues<AMask>().Aggregate((a, b) => a | b));
            // await req.Process();
            // await Task.Delay(TimeSpan.FromSeconds(30));
            // await aniDbUdp.Logout();

            #endregion

            var imptfld = context.ImportFolders.FirstOrDefault();
            if (imptfld is null)
            {
                imptfld = context.ImportFolders.Add(new ImportFolder
                {
                    Name = "test",
                    Path = @"C:\Users\Mike\Desktop\Anime"
                }).Entity;
                context.SaveChanges();
            }
            importer.ScanImportFolder(imptfld.Id);
            importer.PopulateLocalFileAniDbRelations();
            var result = context.LocalFiles.GetByEpisodeId(1).ToList();
        }
    }
}
