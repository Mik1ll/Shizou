using System.Diagnostics;
using Shizou.Data;
using Shizou.Server.RHash;
using Shizou.Server.Services;

namespace Shizou.Tests;

[TestClass]
public class HashTests
{
    [TestMethod]
    public async Task TestHashSpeedAsync()
    {
        var stopWatch = new Stopwatch();
        var file = await CreateTestFileAsync(300, "hashTestFile.bin");

        stopWatch.Start();
        var hashes = await new HashService().GetFileHashesAsync(file, HashIds.Ed2k);
        stopWatch.Stop();
        var elapsed = stopWatch.Elapsed;
        Console.WriteLine($"Took {elapsed}, Hash: {hashes[HashIds.Ed2k]}");
        file.Delete();
    }

    [TestMethod]
    public async Task TestSmallFileAsync()
    {
        var file = await CreateTestFileAsync(1, "hashTestFile.bin");

        var hashes = await new HashService().GetFileHashesAsync(file, HashIds.Ed2k);
        Console.WriteLine($"Small file hash: {hashes[HashIds.Ed2k]}");
        file.Delete();
    }

    [TestMethod]
    public void TestBadHashId()
    {
        var ex = Assert.ThrowsException<ArgumentException>(() => new RHasher(HashIds.Md4).ToString(HashIds.Sha1 | HashIds.Crc32));
        Assert.AreEqual("No hash id set or multiple hash ids set", ex.Message);
    }

    // ReSharper disable once InconsistentNaming
    private static async Task<FileInfo> CreateTestFileAsync(int sizeInMB, string testFileName)
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
