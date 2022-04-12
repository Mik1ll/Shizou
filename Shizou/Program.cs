using System;
using System.IO;
using System.Linq;
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
using Shizou.Options;
using Shizou.Services.Import;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    if (!Directory.Exists(Constants.ApplicationData))
        Directory.CreateDirectory(Constants.ApplicationData);
    if (!File.Exists(ShizouOptions.OptionsPath) || string.IsNullOrWhiteSpace(File.ReadAllText(ShizouOptions.OptionsPath)))
        ShizouOptions.SaveToFile(new ShizouOptions());

    var builder = WebApplication.CreateBuilder();
    builder.Host.ConfigureAppConfiguration(config => config.AddJsonFile(ShizouOptions.OptionsPath, false, true));
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    builder.Services.Configure<ShizouOptions>(builder.Configuration.GetSection(ShizouOptions.Shizou));
    builder.Services.AddControllers();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHostedService<StartupService>();
    builder.Services.AddDbContext<ShizouContext>();
    builder.Services.AddScoped<CommandManager>();
    builder.Services.AddScoped<Importer>();

    builder.Services.AddSingleton<AniDbUdp>();
    builder.Services.AddSingleton<UdpRateLimiter>();

    builder.Services.AddSingleton<CommandProcessor, AniDbUdpProcessor>();
    builder.Services.AddSingleton<CommandProcessor, HashProcessor>();
    builder.Services.AddSingleton<CommandProcessor, AniDbHttpProcessor>();
    builder.Services.AddSingleton(p => (AniDbUdpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
    builder.Services.AddSingleton(p => (HashProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
    builder.Services.AddSingleton(p => (AniDbHttpProcessor)p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));
    builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbUdp));
    builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.Hash));
    builder.Services.AddSingleton<IHostedService>(p => p.GetServices<CommandProcessor>().First(s => s.QueueType == QueueType.AniDbHttp));

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    // app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();


    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<ShizouContext>();
        context.Database.Migrate();
    }

    app.Run();
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
