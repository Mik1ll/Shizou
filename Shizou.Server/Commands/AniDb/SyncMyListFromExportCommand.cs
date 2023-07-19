using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Enums;
using Shizou.Server.AniDbApi.Requests.Udp;

namespace Shizou.Server.Commands.AniDb;

public record SyncMyListFromExportArgs(string Filename) : CommandArgs($"{nameof(SyncMyListFromExportCommand)}_filename={Filename}");

[Command(CommandType.SyncMyListFromExport, CommandPriority.Low, QueueType.General)]
public class SyncMyListFromExportCommand : BaseCommand<SyncMyListFromExportArgs>
{
    private static readonly string MyListName = "mylist.txt";
    private static readonly string ChangelogName = "changelog.txt";
    private readonly ILogger<SyncMyListFromExportCommand> _logger;
    private string? _myList;
    private string? _changelog;
    private DateTimeOffset _exportTime;

    public SyncMyListFromExportCommand(ILogger<SyncMyListFromExportCommand> logger)
    {
        _logger = logger;
    }

    protected override async Task ProcessInner()
    {
        _exportTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(CommandArgs.Filename.Split("-")[0]));
        var file = new FileInfo(Path.Combine(FilePaths.MyListBackupDir, CommandArgs.Filename));
        if (!file.Exists)
        {
            _logger.LogError("Tried to sync from non-existant mylist export");
            Completed = true;
            return;
        }


        _logger.LogInformation("Starting MyList sync from export file \"{Filename}\"", CommandArgs.Filename);
        await using var gzip = new GZipStream(file.OpenRead(), CompressionMode.Decompress);
        using var unzippedStream = new MemoryStream();
        await gzip.CopyToAsync(unzippedStream);
        unzippedStream.Seek(0, SeekOrigin.Begin);
        await using var reader = new TarReader(unzippedStream);
        while (await reader.GetNextEntryAsync() is { DataStream: not null } entry)
            if (entry.Name == MyListName)
                _myList = await new StreamReader(entry.DataStream).ReadToEndAsync();
            else if (entry.Name == ChangelogName)
                _changelog = await new StreamReader(entry.DataStream).ReadToEndAsync();
        if (_myList is null || _changelog is null)
        {
            _logger.LogError("{MyListName} or {ChangelogName} were not found in the archive, aborting sync", MyListName, ChangelogName);
            Completed = true;
            return;
        }
        if (!_changelog.StartsWith($"Simple Text MYLIST Export Format - ChangeLog\nVersion: {MyListExportRequest.TemplateVersion}"))
        {
            _logger.LogError("MyList export did not get expected header/version in changelog, aborting sync");
            Completed = true;
            return;
        }

        Completed = true;
    }
}
