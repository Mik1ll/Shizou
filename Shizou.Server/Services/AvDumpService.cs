using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Models;

namespace Shizou.Server.Services;

public partial class AvDumpService
{
    private readonly ILogger<AvDumpService> _logger;

    [GeneratedRegex("""\P{IsBasicLatin}""")]
    private static partial Regex NonAsciiRegex();

    public AvDumpService(ILogger<AvDumpService> logger) => _logger = logger;

    /// <summary>
    ///     AVDump the file, sends the media data and hashes to AniDB for auto-creqing.
    /// </summary>
    /// <param name="localFile">The file to dump</param>
    /// <param name="username">AniDB username</param>
    /// <param name="udpKey">AniDB api key. This is set in the account settings</param>
    /// <param name="localPort">Local port to use for the connection</param>
    /// <exception cref="NullReferenceException"></exception>
    /// <remarks>Requires .net 6 runtime and AVDump in the <see cref="FilePaths.AvDumpDir" /> directory</remarks>
    public async Task AvDumpFileAsync(LocalFile localFile, string username, string udpKey, int localPort)
    {
        if (localFile.ImportFolder is null) throw new NullReferenceException("Import folder for LocalFile is null, did you include it?");
        var path = Path.Combine(localFile.ImportFolder.Path, localFile.PathTail);
        if (!Path.Exists(path))
        {
            _logger.LogError("File at \"{FilePath}\" does not exist.", path);
            throw new ArgumentException("File does not exist");
        }

        // Create symlink without non-ascii characters because MediaInfo (called by AVDump)
        // does not handle them well on certain systems e.g. dotnet chiseled ubuntu container.
        string? symlinkPath = null;
        if (NonAsciiRegex().IsMatch(path))
        {
            var fileName = Path.GetFileName(localFile.PathTail);
            var asciiName = NonAsciiRegex().Replace(fileName, "_");
            symlinkPath = Path.Combine(Path.GetTempPath(), "shizou", asciiName);
            if (Path.Exists(symlinkPath))
                File.Delete(symlinkPath);
            File.CreateSymbolicLink(symlinkPath, path);
        }

        var avdumpP = NewAvDumpProcess();
        avdumpP.StartInfo.ArgumentList.Add("--ForwardConsoleCursorOnly"); // Prevents progress bars that are incompatible with logging
        avdumpP.StartInfo.ArgumentList.Add("--PrintEd2kLink");
        avdumpP.StartInfo.ArgumentList.Add($"--Auth={username}:{udpKey}");
        avdumpP.StartInfo.ArgumentList.Add($"--LPort={localPort}");
        avdumpP.StartInfo.ArgumentList.Add("--TOut=10:3"); // Timeout 10 seconds between 3 retries
        avdumpP.StartInfo.ArgumentList.Add(symlinkPath ?? path);
        avdumpP.Start();
        var res = await avdumpP.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(res))
            if (res.Contains("Error"))
                _logger.LogError("AVDump returned Error:\n{AvDumpError}", res);
            else
                _logger.LogInformation("AVDump returned:\n{AvDumpResult}", res);
        else
            _logger.LogError("AVDump did not return any text");

        if (symlinkPath is not null && Path.Exists(symlinkPath))
            File.Delete(symlinkPath);
    }

    private Process NewAvDumpProcess()
    {
        var avDumpProcess = new Process();
        avDumpProcess.StartInfo.CreateNoWindow = true;
        avDumpProcess.StartInfo.UseShellExecute = false;
        avDumpProcess.StartInfo.RedirectStandardOutput = true;
        avDumpProcess.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        avDumpProcess.StartInfo.RedirectStandardError = true;
        avDumpProcess.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        avDumpProcess.StartInfo.FileName = "dotnet";
        avDumpProcess.StartInfo.ArgumentList.Add(Path.Combine(FilePaths.AvDumpDir, "AVDump3CL.dll"));
        return avDumpProcess;
    }
}
