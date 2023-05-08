using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou.AniDbApi;
using Shizou.AniDbApi.RateLimiters;
using Shizou.CommandProcessors;
using Shizou.MapperProfiles;
using Shizou.Options;
using Shizou.Services;
using ShizouData;
using ShizouData.Database;
using ShizouData.Enums;

namespace Shizou.Extensions;

public static class WebAppliationBuilderExtensions
{
    public static WebApplicationBuilder AddShizouOptions(this WebApplicationBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.OptionsPath)!);
        if (!File.Exists(FilePaths.OptionsPath))
            new ShizouOptions().SaveToFile();
        builder.Configuration.AddJsonFile(FilePaths.OptionsPath, false, true);
        builder.Services.AddOptions<ShizouOptions>()
            .Bind(builder.Configuration.GetSection(ShizouOptions.Shizou))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return builder;
    }

    public static WebApplicationBuilder AddShizouLogging(this WebApplicationBuilder builder, string logTemplate)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.File(Path.Combine(FilePaths.LogsDir, ".log"), outputTemplate: logTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341")
            .Enrich.FromLogContext());
        return builder;
    }

    public static WebApplicationBuilder AddShizouServices(this WebApplicationBuilder builder)
    {
        var connectionString = @$"Data Source={Path.Combine(FilePaths.ApplicationDataDir, "ShizouDB.sqlite3")};Foreign Keys=True;";
        builder.Services.AddDbContext<ShizouContext>(opts => opts
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging(),
            optionsLifetime: ServiceLifetime.Singleton);

        builder.Services.AddDbContextFactory<ShizouContext>(opts => opts
            .UseSqlite(connectionString)
            .EnableSensitiveDataLogging());

        builder.Services.AddScoped<CommandService>();
        builder.Services.AddScoped<ImportService>();
        return builder;
    }

    public static WebApplicationBuilder AddShizouProcessors(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<CommandProcessor, AniDbUdpProcessor>();
        builder.Services.AddSingleton<CommandProcessor, HashProcessor>();
        builder.Services.AddSingleton<CommandProcessor, AniDbHttpProcessor>();
        builder.Services.AddSingleton(p => (AniDbUdpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
        builder.Services.AddSingleton(p => (HashProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
        builder.Services.AddSingleton(p => (AniDbHttpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
        builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
        return builder;
    }

    public static WebApplicationBuilder AddAniDbServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AniDbUdpState>();
        builder.Services.AddSingleton<UdpRateLimiter>();
        builder.Services.AddSingleton<AniDbHttpState>();
        builder.Services.AddSingleton<HttpRateLimiter>();

        builder.Services.AddHttpClient("gzip")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
        return builder;
    }

    public static WebApplicationBuilder AddShizouApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddAutoMapper(typeof(ShizouProfile));
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(opt =>
        {
            opt.SchemaGeneratorOptions.UseInlineDefinitionsForEnums = true;
            opt.SchemaGeneratorOptions.SupportNonNullableReferenceTypes = true;
        });
        return builder;
    }
}
