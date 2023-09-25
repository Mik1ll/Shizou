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
        var file = await CreateTestFile(300, "hashTestFile.bin");

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
        var file = await CreateTestFile(1, "hashTestFile.bin");

        var hashes = await RHasherService.GetFileHashesAsync(file, RHasherService.HashIds.Ed2k);
        Console.WriteLine($"Small file hash: {hashes[RHasherService.HashIds.Ed2k]}");
        file.Delete();
    }

    // ReSharper disable once InconsistentNaming
    private static async Task<FileInfo> CreateTestFile(int sizeInMB, string testFileName)
    {
        var file = new FileInfo(Path.Combine(FilePaths.ApplicationDataDir, testFileName));
        var data = new byte[sizeInMB * 1024 * 1024];
        var rng = new Random(555);
        rng.NextBytes(data);
        await using var stream = file.Create();
        await stream.WriteAsync(data);
        return file;
    }
}
