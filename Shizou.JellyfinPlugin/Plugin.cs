﻿using System.Globalization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Shizou.HttpClient;
using Shizou.JellyfinPlugin.Configuration;

namespace Shizou.JellyfinPlugin;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ILogger<Plugin> _logger;
    private readonly SocketsHttpHandler _httpHandler;

    private readonly SemaphoreSlim _loggingInLock = new(1, 1);
    private DateTimeOffset? _lastLogin;
    private System.Net.Http.HttpClient? _httpClient;
    private ShizouHttpClient? _shizouHttpClient;

    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger) : base(applicationPaths, xmlSerializer)
    {
        _logger = logger;
        _httpHandler = new SocketsHttpHandler()
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        Instance = this;
    }

    public static Plugin Instance { get; private set; } = null!;

    public override string Name => "Shizou";

    public override Guid Id => Guid.Parse("1E81A180-292D-4523-9D57-D03F5221C2F2");

    public System.Net.Http.HttpClient HttpClient
    {
        get
        {
            GetHttpClient();
            return _httpClient!;
        }
    }

    public ShizouHttpClient ShizouHttpClient
    {
        get
        {
            GetHttpClient();
            return _shizouHttpClient!;
        }
    }

    public async Task LoginAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await _loggingInLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            {
                await _loggingInLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("Obtained login lock after waiting, skipping login");
                return;
            }

            if (_lastLogin is not null && DateTimeOffset.Now < _lastLogin + TimeSpan.FromSeconds(10))
            {
                _logger.LogWarning("Logged in less than 10 seconds ago, not retrying");
                return;
            }

            _logger.LogInformation("Logging in...");
            await ShizouHttpClient.AccountLoginAsync(Configuration.ServerPassword, cancellationToken).ConfigureAwait(false);
            _lastLogin = DateTimeOffset.Now;
            _logger.LogInformation("Successfully logged in");
        }
        finally
        {
            _loggingInLock.Release();
        }
    }

    public IEnumerable<PluginPageInfo> GetPages() =>
    [
        new()
        {
            Name = Name,
            EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
        }
    ];

    private void GetHttpClient()
    {
        var configBaseAddr = new Uri(Configuration.ServerBaseAddress);
        if (_httpClient is not null && _shizouHttpClient is not null && configBaseAddr == _httpClient.BaseAddress)
            return;
        _httpClient?.Dispose();
        _httpClient = new System.Net.Http.HttpClient(_httpHandler, false);
        _httpClient.BaseAddress = configBaseAddr;
        _shizouHttpClient = new ShizouHttpClient(_httpClient);
    }
}
