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

    public async Task AvDumpFileAsync(LocalFile localFile, string username, string udpKey, int localPort)
    {
        if (localFile.ImportFolder is null) throw new NullReferenceException("Import folder for LocalFile is null, did you include it?");
        var avdumpP = NewAvDumpProcess();
        avdumpP.StartInfo.ArgumentList.Add("--ForwardConsoleCursorOnly");
        avdumpP.StartInfo.ArgumentList.Add("--PrintEd2kLink");
        avdumpP.StartInfo.ArgumentList.Add($"--Auth={username}:{udpKey}");
        avdumpP.StartInfo.ArgumentList.Add($"--LPort={localPort}");
        avdumpP.StartInfo.ArgumentList.Add("--TOut=10:3");
        avdumpP.StartInfo.ArgumentList.Add(Path.Combine(localFile.ImportFolder.Path, localFile.PathTail));
        avdumpP.Start();
        var res = await avdumpP.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(res))
            if (res.Contains("Error"))
                _logger.LogError("AVDump returned Error:\n{AvDumpError}", res);
            else
                _logger.LogInformation("AVDump returned:\n{AvDumpResult}", res);
        else
            _logger.LogError("AVDump did not return any text");
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
