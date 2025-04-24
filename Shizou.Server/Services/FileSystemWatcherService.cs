using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shizou.Data.Database;
using Timer = System.Timers.Timer;

namespace Shizou.Server.Services;

public sealed class FileSystemWatcherService(
    IShizouContextFactory contextFactory,
    ILogger<FileSystemWatcherService> logger,
    ImportService importService
) : IDisposable
{
    private readonly ConcurrentDictionary<int, (FileSystemWatcher watcher, Timer timer)> _watchers = [];
    private readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(2);

    public void UpdateWatchedFolders()
    {
        using var context = contextFactory.CreateDbContext();
        var importFolders = context.ImportFolders.AsNoTracking().ToList();
        foreach (var folder in importFolders)
            if (_watchers.TryGetValue(folder.Id, out var watcherTuple))
            {
                if (!folder.Watched)
                    RemoveWatcher(folder.Id);
                else if (folder.Path != watcherTuple.watcher.Path)
                    watcherTuple.watcher.Path = folder.Path;
            }
            else
            {
                if (!folder.Watched)
                    continue;
                var newWatcher = new FileSystemWatcher
                {
                    Path = folder.Path,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
                };
                var newTimer = new Timer(_updateDelay) { AutoReset = false };
                newTimer.Elapsed += (_, _) =>
                {
                    using var timerContext = contextFactory.CreateDbContext();
                    var myFld = timerContext.ImportFolders.AsNoTracking().FirstOrDefault(i => i.Id == folder.Id);
                    if (myFld is { Watched: true })
                        importService.ScanImportFolder(folder.Id);
                    else
                        RemoveWatcher(folder.Id);
                };
                _watchers[folder.Id] = (newWatcher, newTimer);

                void OnChanged(object? sender, FileSystemEventArgs e)
                {
                    newTimer.Stop();
                    newTimer.Start();
                }

                newWatcher.Changed += OnChanged;
                newWatcher.Created += OnChanged;
                newWatcher.Renamed += OnChanged;
                newWatcher.Error += OnError;
            }
    }

    private void OnError(object? sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        logger.LogError(ex, "Error from file watcher: {ExMessage}", ex.Message);
    }

    private void RemoveWatcher(int id)
    {
        if (!_watchers.Remove(id, out var watcherTuple)) return;
        watcherTuple.watcher.Dispose();
        watcherTuple.timer.Dispose();
    }

    public void Dispose()
    {
        foreach (var id in _watchers.Keys)
            RemoveWatcher(id);
    }
}
