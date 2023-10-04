namespace Shizou.Tests;

[TestClass]
public class DatabaseTests : SeededDatabaseTests
{
    [TestMethod]
    public void TestDateTimeConversion()
    {
        using var context = GetContext();
        var result = context.AniDbAnimes.Where(a => a.Updated < DateTime.UtcNow).ToList();
        Assert.IsNotNull(result);
    }


    [TestMethod]
    public void TestQueryables()
    {
        using var context = GetContext();
        var result = context.AniDbFiles.Where(f => f.LocalFile != null).Select(f => f.Id)
            .Union(context.EpisodeWatchedStates.Where(ws => ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any())
                .Select(ws => ws.AniDbFileId!.Value));
        Assert.IsNotNull(result);
    }
}
