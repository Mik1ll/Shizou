using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shizou.Data;
using Shizou.Data.Models;

namespace Shizou.Server.Services;

public class AvDumpService
{
    private readonly ILogger<AvDumpService> _logger;

    public AvDumpService(ILogger<AvDumpService> logger) => _logger = logger;

    public async Task RunAvDumpAsync(LocalFile localFile)
    {
        if (localFile.ImportFolder is null) throw new NullReferenceException("Import folder for LocalFile is null, did you include it?");
        var avdumpP = NewAvDumpProcess();
        avdumpP.StartInfo.ArgumentList.Add("--Cons=ED2K");
        avdumpP.StartInfo.ArgumentList.Add("--PrintHashes");
        avdumpP.StartInfo.ArgumentList.Add("--HideBuffers");
        avdumpP.StartInfo.ArgumentList.Add("--HideFileProgress");
        avdumpP.StartInfo.ArgumentList.Add("--HideTotalProgress");
        avdumpP.StartInfo.ArgumentList.Add("--ForwardConsoleCursorOnly");
        avdumpP.StartInfo.ArgumentList.Add(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        avdumpP.Start();
        var res = await avdumpP.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var err = await avdumpP.StandardError.ReadToEndAsync().ConfigureAwait(false);
        _logger.LogInformation("AVDump returned:\n{AvDumpResult}", res);
        if (!string.IsNullOrWhiteSpace(err))
            _logger.LogWarning("AVDump returned Error:\n{AvDumpError}", err);
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
        avDumpProcess.StartInfo.WorkingDirectory = FilePaths.InstallDir;
        avDumpProcess.StartInfo.FileName = "dotnet";
        avDumpProcess.StartInfo.ArgumentList.Add(Path.Combine(FilePaths.AvDumpDir, "AVDump3CL.dll"));
        return avDumpProcess;
    }
}
