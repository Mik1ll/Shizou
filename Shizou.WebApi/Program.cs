using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
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

    var builder = WebApplication.CreateBuilder(
        // Swagger openapi json won't generate without setting the name here for some reason
        new WebApplicationOptions { ApplicationName = Assembly.GetExecutingAssembly().GetName().Name });

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
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures) Log.Logger.Error("{Failure}", failure);
}
finally
{
    if (hasHandle)
        appMutex.ReleaseMutex();
}
