using System.IO;
using Shizou.Server.SerilogSinks;

namespace Shizou.Server.Services;

public class LogFileService
{
    public CaptureFilePathHook FilePathHook { get; } = new();

    public FileInfo? CurrentFile => FilePathHook.Path is { } path ? new FileInfo(path) : null;
}
