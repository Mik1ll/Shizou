using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Server.AniDbApi;
using Shizou.Server.AniDbApi.RateLimiters;
using Shizou.Server.AniDbApi.Requests.Http;
using Shizou.Server.AniDbApi.Requests.Http.Interfaces;
using Shizou.Server.AniDbApi.Requests.Image;
using Shizou.Server.AniDbApi.Requests.Udp;
using Shizou.Server.AniDbApi.Requests.Udp.Interfaces;
using Shizou.Server.CommandProcessors;
using Shizou.Server.Commands;
using Shizou.Server.Commands.AniDb;
using Shizou.Server.Options;
using Shizou.Server.Services;
using Shizou.Server.SwaggerFilters;
using Swashbuckle.AspNetCore.Filters;
using UdpAnimeRequest = Shizou.Server.AniDbApi.Requests.Udp.AnimeRequest;
using IUdpAnimeRequest = Shizou.Server.AniDbApi.Requests.Udp.Interfaces.IAnimeRequest;
using HttpAnimeRequest = Shizou.Server.AniDbApi.Requests.Http.AnimeRequest;
using IHttpAnimeRequest = Shizou.Server.AniDbApi.Requests.Http.Interfaces.IAnimeRequest;

namespace Shizou.Server.Extensions;

public static class InitializationExtensions
{
    public static WebApplicationBuilder AddShizouOptions(this WebApplicationBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePaths.OptionsPath)!);
        if (!File.Exists(FilePaths.OptionsPath))
            new ShizouOptions().SaveToFile();
        ShizouOptions.GenerateSchema();
        builder.Configuration.AddJsonFile(FilePaths.OptionsPath, false, true);
        builder.Services.AddOptions<ShizouOptions>()
            .BindConfiguration(ShizouOptions.Shizou)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        var certDir = new DirectoryInfo(FilePaths.CertificateDir);
        certDir.Create();
        if (!builder.Environment.IsDevelopment())
        {
            var certExts = new[] { ".pem", ".cer", ".crt", ".cert", ".pfx", ".p12", ".pkcs12" };
            if (builder.Configuration.GetSection("Kestrel:Certificates:Default:Path").Value is null)
            {
                var files = certDir.GetFiles();
                var certs = files.Where(f => certExts.Any(ex => f.Name.ToLower().EndsWith(ex))).ToList();
                if (certs.Count > 1)
                    throw new InvalidOperationException(
                        $"Only one certificate store file allowed in \"{FilePaths.CertificateDir}\", if there is a PEM key file, change the file extension to .key");
                if (certs.FirstOrDefault() is { } certFile)
                    builder.Configuration["Kestrel:Certificates:Default:Path"] = certFile.FullName;

                if (files.FirstOrDefault(f => f.Name.ToLower().EndsWith(".key")) is { } keyFile)
                    builder.Configuration["Kestrel:Certificates:Default:KeyPath"] = keyFile.FullName;
            }

            if (builder.Configuration.GetSection("Kestrel:Certificates:Default:Path").Value is null)
                throw new InvalidOperationException(
                    $@"No certificate found, please add a SSL X.509 ASN.1 Certificate in PEM or PKCS#12 format:
  Must end with one of these extensions: {string.Join(", ", certExts)}
  If PEM key is a separate file, must end with .key
  Sourced from either:
    ""{FilePaths.CertificateDir}""
     env variable ASPNETCORE_Kestrel__Certificates__Default__Path and optionally ASPNETCORE_Kestrel__Certificates__Default__KeyPath
  If key is password protected, set env variable ASPNETCORE_Kestrel__Certificates__Default__Password");
        }

        return builder;
    }

    public static WebApplicationBuilder AddShizouLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ConfigureSerilog());
        return builder;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        builder.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            if (!context.Request.Path.StartsWithSegments(Constants.ApiPrefix))
            {
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-XSS-Protection", "0");
                context.Response.Headers.Append("Content-Security-Policy", "base-uri 'self';" +
                                                                           "default-src 'self';" +
                                                                           "img-src data: https: blob:;" +
                                                                           "object-src 'none';" +
                                                                           "script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval';" +
                                                                           "style-src 'self' 'unsafe-inline';" +
                                                                           "font-src 'self' data:;" +
                                                                           "upgrade-insecure-requests");
            }
            else
            {
                context.Response.Headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'");
            }

            await next().ConfigureAwait(false);
        });
        return builder;
    }

    public static WebApplicationBuilder AddWorkerServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseWindowsService(cfg => cfg.ServiceName = "Shizou")
            .UseSystemd();
        return builder;
    }

    public static WebApplication MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<IShizouContext>();
        context.Database.Migrate();
        using var identityContext = scope.ServiceProvider.GetRequiredService<AuthContext>();
        identityContext.Database.Migrate();
        return app;
    }

    public static LoggerConfiguration ConfigureSerilog(this LoggerConfiguration cfg)
    {
        var logTemplate = "{Timestamp:HH:mm:ss} {Level:u3} | {SourceContext} {Message:lj}{NewLine:1}{Exception:1}";
        return cfg
            .WriteTo.Console(outputTemplate: logTemplate)
            .WriteTo.File(Path.Combine(FilePaths.LogsDir, ".log"), outputTemplate: logTemplate, rollingInterval: RollingInterval.Day)
            .WriteTo.Seq("http://localhost:5341")
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithThreadName()
            .Filter.ByExcluding(logEvent => logEvent.IsSuppressed());
    }

    public static IServiceCollection AddShizouServices(this IServiceCollection services)
    {
        services.AddDbContextFactory<ShizouContext>((provider, opts) =>
            {
                var username = provider.GetService<IOptionsMonitor<ShizouOptions>>()?.CurrentValue.AniDb.Username ?? string.Empty;
                opts.UseSqlite(new SqliteConnectionStringBuilder
                    {
                        DataSource = FilePaths.DatabasePath(username),
                        ForeignKeys = true,
                        Cache = SqliteCacheMode.Private,
                        Pooling = false
                    }.ConnectionString)
                    .EnableSensitiveDataLogging();
            })
            .AddDbContext<AuthContext>()
            .AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AuthContext>()
            .AddDefaultTokenProviders().Services
            // Fix web api endpoints redirecting when Blazor is hosted
            // https://github.com/dotnet/aspnetcore/issues/9039#issuecomment-1026158591
            .ConfigureApplicationCookie(opts =>
            {
                var loginEvent = opts.Events.OnRedirectToLogin;
                opts.Events.OnRedirectToLogin = async ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments(Constants.ApiPrefix) && ctx.Response.StatusCode == StatusCodes.Status200OK)
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    else
                        await loginEvent(ctx).ConfigureAwait(false);
                };
                var accDenEvent = opts.Events.OnRedirectToAccessDenied;
                opts.Events.OnRedirectToAccessDenied = async ctx =>
                {
                    if (ctx.Request.Path.StartsWithSegments(Constants.ApiPrefix) && ctx.Response.StatusCode == StatusCodes.Status200OK)
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    else
                        await accDenEvent(ctx).ConfigureAwait(false);
                };
                opts.Cookie.Name = IdentityConstants.ApplicationScheme;
            })
            .AddAuthorization()
            .AddScoped<IShizouContext, ShizouContext>(p => p.GetRequiredService<ShizouContext>())
            .AddSingleton<IShizouContextFactory, ShizouContextFactory>(p =>
                new ShizouContextFactory(p.GetRequiredService<IDbContextFactory<ShizouContext>>()))
            .AddHostedService<StartupService>()
            .AddTransient<HashCommand>()
            .AddTransient<NoopCommand>()
            .AddTransient<AnimeCommand>()
            .AddTransient<ProcessCommand>()
            .AddTransient<SyncMyListCommand>()
            .AddTransient<UpdateMyListCommand>()
            .AddTransient<AddMyListCommand>()
            .AddTransient<UpdateMyListByEpisodeCommand>()
            .AddTransient<AddMissingMyListEntriesCommand>()
            .AddTransient<GetImageCommand>()
            .AddTransient<RestoreMyListBackupCommand>()
            .AddTransient<ExtractExtraDataCommand>()
            .AddTransient<GetAnimeByEpisodeIdCommand>()
            .AddTransient<GetAnimeTitlesCommand>()
            .AddTransient<AvDumpCommand>()
            .AddTransient<CreatorCommand>()
            .AddSingleton<CommandService>()
            .AddHostedService<CommandService>(p => p.GetRequiredService<CommandService>())
            .AddTransient<ImportService>()
            .AddTransient<WatchStateService>()
            .AddTransient<ImageService>()
            .AddTransient<HashService>()
            .AddTransient<SubtitleService>()
            .AddSingleton<IAnimeTitleSearchService, AnimeTitleSearchService>() // Uses an in memory cache
            .AddTransient<MyAnimeListService>()
            .AddTransient<FfmpegService>()
            .AddTransient<ManualLinkService>()
            .AddTransient<AnimeService>()
            .AddTransient<AvDumpService>()
            .AddTransient<SymbolicCollectionViewService>();
        return services;
    }

    public static IServiceCollection AddShizouProcessors(this IServiceCollection services)
    {
        services.AddProcessor<AniDbUdpProcessor>()
            .AddProcessor<HashProcessor>()
            .AddProcessor<AniDbHttpProcessor>()
            .AddProcessor<GeneralProcessor>()
            .AddProcessor<ImageProcessor>()
            .Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(30));
        return services;
    }

    public static IServiceCollection AddAniDbServices(this IServiceCollection services)
    {
        services.AddSingleton<AniDbUdpState>()
            .AddSingleton<UdpRateLimiter>()
            .AddSingleton<AniDbHttpState>()
            .AddSingleton<HttpRateLimiter>()
            .AddSingleton<ImageRateLimiter>()
            .AddTransient<IUdpAnimeRequest, UdpAnimeRequest>()
            .AddGenericFactory<IAuthRequest, AuthRequest>()
            .AddGenericFactory<ILogoutRequest, LogoutRequest>()
            .AddTransient<IEpisodeRequest, EpisodeRequest>()
            .AddTransient<IFileRequest, FileRequest>()
            .AddTransient<IGenericRequest, GenericRequest>()
            .AddTransient<IMyListAddRequest, MyListAddRequest>()
            .AddTransient<IPingRequest, PingRequest>()
            .AddTransient<IMyListEntryRequest, MyListEntryRequest>()
            .AddTransient<IHttpAnimeRequest, HttpAnimeRequest>()
            .AddTransient<IMyListRequest, MyListRequest>()
            .AddTransient<ICreatorRequest, CreatorRequest>()
            .AddTransient<ImageRequest>()
            .AddTransient<IUserRequest, UserRequest>()
            .AddHttpClient("gzip")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip });
        return services;
    }

    public static IServiceCollection AddShizouApiServices(this IServiceCollection services)
    {
        services.AddControllers(opts => opts.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())).Services
            .AddSwaggerGen(opt =>
            {
                opt.SchemaGeneratorOptions.UseInlineDefinitionsForEnums = true;
                opt.SchemaGeneratorOptions.SupportNonNullableReferenceTypes = true;
                opt.SchemaGeneratorOptions.UseAllOfToExtendReferenceSchemas = true; // Fix nullable reference types not appearing as nullable
                opt.CustomOperationIds(e => $"ShizouHttpClient_{e.ActionDescriptor.RouteValues["controller"]}{e.ActionDescriptor.RouteValues["action"]}");
                opt.OrderActionsBy(apiDesc =>
                    $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.RelativePath?.Length ?? 0:d3}_{apiDesc.HttpMethod switch { "GET" => "0", "PUT" => "1", "POST" => "2", "DELETE" => "3", _ => "4" }}");
                opt.EnableAnnotations();
                // opt.AddSecurityDefinition(IdentityConstants.ApplicationScheme, new OpenApiSecurityScheme
                // {
                //     Type = SecuritySchemeType.ApiKey,
                //     Name = IdentityConstants.ApplicationScheme,
                //     In = ParameterLocation.Cookie,
                // });
                opt.OperationFilter<SecurityOperationFilter>();
                opt.OperationFilter<AddResponseHeadersFilter>();
            })
            .AddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();
        return services;
    }

    public static IApplicationBuilder UseIdentityCookieParameter(this IApplicationBuilder app)
    {
        return app.Use((context, next) =>
        {
            if (!context.Request.Cookies.ContainsKey(IdentityConstants.ApplicationScheme) &&
                context.Request.Query.TryGetValue(IdentityConstants.ApplicationScheme, out var identityParam))
                context.Request.Headers.Cookie = new StringValues(
                    (string.IsNullOrWhiteSpace(context.Request.Headers.Cookie) ? string.Empty : $"{context.Request.Headers.Cookie}; ") +
                    $"{IdentityConstants.ApplicationScheme}={identityParam}");

            return next.Invoke();
        });
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddProcessor<TProcessor>(this IServiceCollection services)
        where TProcessor : CommandProcessor
    {
        services
            .AddSingleton<TProcessor>()
            .AddHostedService<TProcessor>(p => p.GetRequiredService<TProcessor>())
            .AddSingleton<CommandProcessor, TProcessor>(p => p.GetRequiredService<TProcessor>());
        return services;
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private static IServiceCollection AddGenericFactory<TInterface, TImplementation>(this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddTransient<TInterface, TImplementation>()
            .AddSingleton<Func<TInterface>>(x => x.GetRequiredService<TInterface>);
        return services;
    }

    // ReSharper disable once UnusedMember.Local
    private static IServiceCollection AddFactory<TInterface, TImplementation, TFactoryInterface, TFactoryImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
        where TFactoryInterface : class
        where TFactoryImplementation : class, TFactoryInterface
    {
        services.AddTransient<TInterface, TImplementation>()
            .AddSingleton<Func<TInterface>>(x => x.GetRequiredService<TInterface>)
            .AddSingleton<TFactoryInterface, TFactoryImplementation>();
        return services;
    }
}
