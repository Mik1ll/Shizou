using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou.Options;

namespace Shizou
{
    public class Program
    {
        public static readonly string ApplicationData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Shizou");
        public static readonly string TempFilePath = Path.Combine(ApplicationData, "Temp");

        public static readonly string HttpCachePath = Path.Combine(ApplicationData, "HTTPAnime");

        public static int Main(string[] args)
        {
            try
            {
                if (!Directory.Exists(ApplicationData))
                    Directory.CreateDirectory(ApplicationData);
                if (!File.Exists(ShizouOptions.OptionsPath) || string.IsNullOrWhiteSpace(File.ReadAllText(ShizouOptions.OptionsPath)))
                    ShizouOptions.SaveSettingsToFile(new ShizouOptions());

                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled Exception");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration, "Serilog"))
                .ConfigureAppConfiguration(config => config.AddJsonFile(ShizouOptions.OptionsPath, false, true))
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .ConfigureServices(services => services.AddHostedService<StartupService>());
        }
    }
}
