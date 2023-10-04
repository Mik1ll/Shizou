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
        var result = from ws in context.EpisodeWatchedStates
            where ws.MyListId == null && ws.AniDbFileId != null && ws.AniDbEpisode.ManualLinkLocalFiles.Any()
            select new { Fid = ws.AniDbFileId!.Value, ws.Watched, ws.WatchedUpdated };
        Assert.IsNotNull(result);
    }
}
