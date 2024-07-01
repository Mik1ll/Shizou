using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Shizou.Data;
using Shizou.Server.Extensions;

Log.Logger = new LoggerConfiguration()
    .ConfigureSerilog()
    .CreateBootstrapLogger();

Directory.CreateDirectory(FilePaths.ApplicationDataDir);

using var appMutex = new Mutex(false, Constants.AppLockName);
var hasHandle = false;
try
{
    try
    {
        hasHandle = appMutex.WaitOne(2000, false);
        if (!hasHandle)
        {
            Log.Logger.Error("Only one instance of Shizou may run per user");
            return;
        }
    }
    catch (AbandonedMutexException)
    {
        hasHandle = true;
    }

    var builder = WebApplication.CreateBuilder();

    builder.AddShizouOptions()
        .AddShizouLogging()
        .AddWorkerServices();

    builder.Services
        .AddShizouServices()
        .AddShizouProcessors()
        .AddAniDbServices()
        .AddShizouApiServices();

    builder.Services.AddHsts(cfg => cfg.MaxAge = TimeSpan.FromSeconds(15_768_000));

    var app = builder.Build();

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.All
    });

    if (!app.Environment.IsDevelopment()) app.UseHsts();


    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseRouting();

    app.UseIdentityCookieParameter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSecurityHeaders();

    app.MapControllers();

    app.MigrateDatabase();

    app.Run();
}
finally
{
    if (hasHandle)
        appMutex.ReleaseMutex();
}
