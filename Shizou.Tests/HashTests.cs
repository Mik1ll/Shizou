using System.Diagnostics;
using Shizou.Data;
using Shizou.Server.Services;

namespace Shizou.Tests;

[TestClass]
public class HashTests
{
    [TestMethod]
    public async Task TestHashSpeed()
    {
        var stopWatch = new Stopwatch();
        var file = new FileInfo(Path.Combine(FilePaths.ApplicationDataDir, "hashTestFile.bin"));
        var data = new byte[300 * 1024 * 1024];
        var rng = new Random(555);
        rng.NextBytes(data);
        await using (var stream = file.Create())
        {
            await stream.WriteAsync(data);
        }

        stopWatch.Start();
        var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2k);
        stopWatch.Stop();
        var elapsed = stopWatch.Elapsed;
        Console.WriteLine($"Took {elapsed}, Hash: {hashes[RHasherService.HashIds.Ed2k]}");
        file.Delete();
    }

    [TestMethod]
    public async Task TestSmallFile()
    {
        var file = new FileInfo(Path.Combine(FilePaths.ApplicationDataDir, "hashTestFile.bin"));
        var data = new byte[512];
        var rng = new Random(555);
        rng.NextBytes(data);
        await using (var stream = file.Create())
        {
            await stream.WriteAsync(data);
        }

        var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2k);
        Console.WriteLine($"Small file hash: {hashes[RHasherService.HashIds.Ed2k]}");
        file.Delete();
    }
}
