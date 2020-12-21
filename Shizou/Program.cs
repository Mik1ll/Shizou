using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Shizou
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration, "Serilog"))
                .ConfigureServices((context, service) =>
                {
                    HostConfig.CertPath = context.Configuration["CertPath"];
                    HostConfig.CertPassword = context.Configuration["CertPassword"];
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(opt =>
                    {
                        opt.ListenAnyIP(5000);
                        opt.ListenAnyIP(5001, listopt =>
                        {
                            listopt.UseHttps(HostConfig.CertPath, HostConfig.CertPassword);
                        });
                    });
                    webBuilder.UseStartup<Startup>();
                });

    }

    public static class HostConfig
    {
        public static string CertPath { get; set; }
        public static string CertPassword { get; set; }
    }
}
