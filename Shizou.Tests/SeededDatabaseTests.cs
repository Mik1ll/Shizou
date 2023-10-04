using Microsoft.EntityFrameworkCore;
using Shizou.Data;
using Shizou.Data.Database;
using Shizou.Data.Enums;
using Shizou.Data.Models;

namespace Shizou.Tests;

[TestClass]
public class SeededDatabaseTests
{
    private static readonly string ConnectionString = $"Data Source={Path.Combine(FilePaths.ApplicationDataDir, "ShizouTestDB.sqlite3")};Foreign Keys=True;";

    private static readonly object Lock = new();
    private static bool _databaseInitialized;

    public SeededDatabaseTests()
    {
        lock (Lock)
        {
            if (!_databaseInitialized)
            {
                using var context = GetContext();
                context.Database.EnsureDeleted();
                context.Database.Migrate();
                context.AniDbAnimes.AddRange(new AniDbAnime
                {
                    Id = 1,
                    Title = "blah",
                    AnimeType = AnimeType.TvSeries,
                    EpisodeCount = 12,
                    AirDate = "2022-02-15",
                    EndDate = null,
                    Description = "testdescription",
                    Restricted = false,
                    ImageFilename = null,
                    Updated = default
                });
                var file = context.AniDbFiles.Add(new AniDbFile
                {
                    Id = 24,
                    Ed2k = "asdf",
                    Crc = null,
                    Md5 = null,
                    Sha1 = null,
                    FileSize = 0,
                    DurationSeconds = null,
                    Source = null,
                    Updated = default,
                    FileVersion = 0,
                    FileName = "blah",
                    Censored = null,
                    Deprecated = false,
                    Chaptered = false,
                    AniDbGroupId = null,
                    AniDbGroup = null,
                    LocalFile = null,
                    Video = null,
                    Audio = new List<AniDbAudio>(),
                    Subtitles = new List<AniDbSubtitle>(),
                    FileWatchedState = new FileWatchedState
                    {
                        AniDbFileId = 24,
                        Watched = false,
                        WatchedUpdated = null,
                        MyListId = null
                    }
                }).Entity;
                context.LocalFiles.AddRange(new LocalFile
                {
                    Id = 23,
                    Ed2k = "asdf",
                    Crc = "blah",
                    FileSize = 0,
                    Signature = "null",
                    Ignored = false,
                    PathTail = "wijw",
                    Updated = null,
                    ImportFolderId = null,
                    ImportFolder = null,
                    ManualLinkEpisodeId = null,
                    ManualLinkEpisode = null,
                    AniDbFile = file
                }, new LocalFile
                {
                    Id = 12,
                    Ed2k = "null",
                    Crc = "null",
                    FileSize = 0,
                    Signature = "hiuhuh",
                    Ignored = false,
                    PathTail = "null",
                    Updated = null,
                    ImportFolderId = null,
                    ImportFolder = null,
                    ManualLinkEpisodeId = null,
                    ManualLinkEpisode = null
                });

                context.SaveChanges();

                _databaseInitialized = true;
            }
        }
    }

    protected static ShizouContext GetContext()
    {
        return new ShizouContext(new DbContextOptionsBuilder<ShizouContext>().UseSqlite(ConnectionString).Options);
    }
}
