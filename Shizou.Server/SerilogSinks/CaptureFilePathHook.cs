using System.IO;
using System.Text;
using Serilog.Sinks.File;

namespace Shizou.Server.SerilogSinks;

public class CaptureFilePathHook : FileLifecycleHooks
{
    public string? Path { get; private set; }

    public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
    {
        Path = path;
        return base.OnFileOpened(path, underlyingStream, encoding);
    }
}
