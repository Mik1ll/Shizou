using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Shizou;
using Shizou.AniDbApi;
using Shizou.CommandProcessors;
using Shizou.Commands;
using Shizou.Database;
using Shizou.MapperProfiles;
using Shizou.Options;
using Shizou.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}")
    .CreateBootstrapLogger();

try
{
    if (!Directory.Exists(Constants.ApplicationData))
        Directory.CreateDirectory(Constants.ApplicationData);
    if (!File.Exists(ShizouOptions.OptionsPath) || string.IsNullOrWhiteSpace(File.ReadAllText(ShizouOptions.OptionsPath)))
        ShizouOptions.SaveToFile(new ShizouOptions());

    var builder = WebApplication.CreateBuilder();
    builder.Configuration.AddJsonFile(ShizouOptions.OptionsPath, false, true);
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}")
        .WriteTo.File(Path.Combine(Constants.ApplicationData, "logs", ".log"),
            outputTemplate: "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}",
            rollingInterval: RollingInterval.Day)
        .WriteTo.Seq("http://localhost:5341")
        .Enrich.FromLogContext());

    builder.Services.AddAutoMapper(typeof(ShizouProfile));

    builder.Services.Configure<ShizouOptions>(builder.Configuration.GetSection(ShizouOptions.Shizou));
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen(opt =>
    {
        opt.SchemaGeneratorOptions.UseInlineDefinitionsForEnums = true;
        opt.SchemaGeneratorOptions.SupportNonNullableReferenceTypes = true;
    });
    builder.Services.AddHostedService<StartupService>();
    builder.Services.AddDbContext<ShizouContext>();
    builder.Services.AddScoped<CommandManager>();
    builder.Services.AddScoped<ImportService>();

    builder.Services.AddSingleton<AniDbUdp>();
    builder.Services.AddSingleton<UdpRateLimiter>();

    AddProcessors(builder);

    builder.Services.AddHttpClient("gzip")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();


    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ShizouContext>();
        context.Database.Migrate();

        var mapper = services.GetRequiredService<IMapper>();
        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }

    app.Run();
}
catch (HostAbortedException)
{
    throw;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}

void AddProcessors(WebApplicationBuilder webApplicationBuilder)
{
    webApplicationBuilder.Services.AddSingleton<CommandProcessor, AniDbUdpProcessor>();
    webApplicationBuilder.Services.AddSingleton<CommandProcessor, HashProcessor>();
    webApplicationBuilder.Services.AddSingleton<CommandProcessor, AniDbHttpProcessor>();
    webApplicationBuilder.Services.AddSingleton(p => (AniDbUdpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
    webApplicationBuilder.Services.AddSingleton(p => (HashProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
    webApplicationBuilder.Services.AddSingleton(p => (AniDbHttpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
    webApplicationBuilder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
    webApplicationBuilder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
    webApplicationBuilder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
}
