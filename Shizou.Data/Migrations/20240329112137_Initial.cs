using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AniDbAnimeRelations",
                columns: table => new
                {
                    AnimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToAnimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbAnimeRelations", x => new { x.AnimeId, x.ToAnimeId, x.RelationType });
                });

            migrationBuilder.CreateTable(
                name: "AniDbAnimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleTranscription = table.Column<string>(type: "TEXT", nullable: false),
                    TitleOriginal = table.Column<string>(type: "TEXT", nullable: true),
                    TitleEngish = table.Column<string>(type: "TEXT", nullable: true),
                    AnimeType = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AirDate = table.Column<string>(type: "TEXT", nullable: true),
                    EndDate = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Restricted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImageFilename = table.Column<string>(type: "TEXT", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AniDbUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbAnimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AniDbGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnimeFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Criteria = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnimeFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommandRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueType = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandId = table.Column<string>(type: "TEXT", nullable: false),
                    CommandArgs = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    ScanOnImport = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFolders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MalAnimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    AnimeType = table.Column<string>(type: "TEXT", nullable: false),
                    EpisodeCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Status_State = table.Column<int>(type: "INTEGER", nullable: true),
                    Status_WatchedEpisodes = table.Column<int>(type: "INTEGER", nullable: true),
                    Status_Updated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MalAnimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NextRunTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RunsLeft = table.Column<int>(type: "INTEGER", nullable: true),
                    FrequencyMinutes = table.Column<double>(type: "REAL", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueType = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandId = table.Column<string>(type: "TEXT", nullable: false),
                    CommandArgs = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledCommands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timers",
                columns: table => new
                {
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ExtraId = table.Column<int>(type: "INTEGER", nullable: false),
                    Expires = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timers", x => new { x.Type, x.ExtraId });
                });

            migrationBuilder.CreateTable(
                name: "AniDbEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    TitleEnglish = table.Column<string>(type: "TEXT", nullable: false),
                    TitleTranscription = table.Column<string>(type: "TEXT", nullable: true),
                    TitleOriginal = table.Column<string>(type: "TEXT", nullable: true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeType = table.Column<int>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    AirDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AniDbAnimeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AniDbEpisodes_AniDbAnimes_AniDbAnimeId",
                        column: x => x.AniDbAnimeId,
                        principalTable: "AniDbAnimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AniDbFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 21, nullable: false),
                    Ed2k = table.Column<string>(type: "TEXT", nullable: true),
                    Crc = table.Column<string>(type: "TEXT", nullable: true),
                    Md5 = table.Column<string>(type: "TEXT", nullable: true),
                    Sha1 = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FileVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    Censored = table.Column<bool>(type: "INTEGER", nullable: true),
                    Deprecated = table.Column<bool>(type: "INTEGER", nullable: true),
                    Chaptered = table.Column<bool>(type: "INTEGER", nullable: true),
                    AniDbGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    Audio = table.Column<string>(type: "TEXT", nullable: true),
                    Subtitles = table.Column<string>(type: "TEXT", nullable: true),
                    Video = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AniDbFiles_AniDbGroups_AniDbGroupId",
                        column: x => x.AniDbGroupId,
                        principalTable: "AniDbGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MalAniDbXrefs",
                columns: table => new
                {
                    MalAnimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbAnimeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MalAniDbXrefs", x => new { x.MalAnimeId, x.AniDbAnimeId });
                    table.ForeignKey(
                        name: "FK_MalAniDbXrefs_AniDbAnimes_AniDbAnimeId",
                        column: x => x.AniDbAnimeId,
                        principalTable: "AniDbAnimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MalAniDbXrefs_MalAnimes_MalAnimeId",
                        column: x => x.MalAnimeId,
                        principalTable: "MalAnimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AniDbEpisodeFileXrefs",
                columns: table => new
                {
                    AniDbEpisodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbEpisodeFileXrefs", x => new { x.AniDbEpisodeId, x.AniDbFileId });
                    table.ForeignKey(
                        name: "FK_AniDbEpisodeFileXrefs_AniDbEpisodes_AniDbEpisodeId",
                        column: x => x.AniDbEpisodeId,
                        principalTable: "AniDbEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AniDbEpisodeFileXrefs_AniDbFiles_AniDbFileId",
                        column: x => x.AniDbFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileWatchedStates",
                columns: table => new
                {
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Watched = table.Column<bool>(type: "INTEGER", nullable: false),
                    WatchedUpdated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MyListId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileWatchedStates", x => x.AniDbFileId);
                    table.ForeignKey(
                        name: "FK_FileWatchedStates_AniDbFiles_AniDbFileId",
                        column: x => x.AniDbFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HangingEpisodeFileXrefs",
                columns: table => new
                {
                    AniDbEpisodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbNormalFileId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HangingEpisodeFileXrefs", x => new { x.AniDbEpisodeId, x.AniDbNormalFileId });
                    table.ForeignKey(
                        name: "FK_HangingEpisodeFileXrefs_AniDbFiles_AniDbNormalFileId",
                        column: x => x.AniDbNormalFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocalFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Ed2k = table.Column<string>(type: "TEXT", nullable: false),
                    Crc = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Signature = table.Column<string>(type: "TEXT", nullable: false),
                    Ignored = table.Column<bool>(type: "INTEGER", nullable: false),
                    PathTail = table.Column<string>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ImportFolderId = table.Column<int>(type: "INTEGER", nullable: true),
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocalFiles_AniDbFiles_AniDbFileId",
                        column: x => x.AniDbFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LocalFiles_ImportFolders_ImportFolderId",
                        column: x => x.ImportFolderId,
                        principalTable: "ImportFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AniDbEpisodeFileXrefs_AniDbFileId",
                table: "AniDbEpisodeFileXrefs",
                column: "AniDbFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbEpisodes_AniDbAnimeId",
                table: "AniDbEpisodes",
                column: "AniDbAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbFiles_AniDbGroupId",
                table: "AniDbFiles",
                column: "AniDbGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbFiles_Ed2k",
                table: "AniDbFiles",
                column: "Ed2k",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommandRequests_CommandId",
                table: "CommandRequests",
                column: "CommandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileWatchedStates_MyListId",
                table: "FileWatchedStates",
                column: "MyListId");

            migrationBuilder.CreateIndex(
                name: "IX_HangingEpisodeFileXrefs_AniDbNormalFileId",
                table: "HangingEpisodeFileXrefs",
                column: "AniDbNormalFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportFolders_Name",
                table: "ImportFolders",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportFolders_Path",
                table: "ImportFolders",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_AniDbFileId",
                table: "LocalFiles",
                column: "AniDbFileId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_Ed2k",
                table: "LocalFiles",
                column: "Ed2k",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_ImportFolderId_PathTail",
                table: "LocalFiles",
                columns: new[] { "ImportFolderId", "PathTail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalFiles_Signature",
                table: "LocalFiles",
                column: "Signature",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MalAniDbXrefs_AniDbAnimeId",
                table: "MalAniDbXrefs",
                column: "AniDbAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledCommands_CommandId",
                table: "ScheduledCommands",
                column: "CommandId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AniDbAnimeRelations");

            migrationBuilder.DropTable(
                name: "AniDbEpisodeFileXrefs");

            migrationBuilder.DropTable(
                name: "AnimeFilters");

            migrationBuilder.DropTable(
                name: "CommandRequests");

            migrationBuilder.DropTable(
                name: "FileWatchedStates");

            migrationBuilder.DropTable(
                name: "HangingEpisodeFileXrefs");

            migrationBuilder.DropTable(
                name: "LocalFiles");

            migrationBuilder.DropTable(
                name: "MalAniDbXrefs");

            migrationBuilder.DropTable(
                name: "ScheduledCommands");

            migrationBuilder.DropTable(
                name: "Timers");

            migrationBuilder.DropTable(
                name: "AniDbEpisodes");

            migrationBuilder.DropTable(
                name: "AniDbFiles");

            migrationBuilder.DropTable(
                name: "ImportFolders");

            migrationBuilder.DropTable(
                name: "MalAnimes");

            migrationBuilder.DropTable(
                name: "AniDbAnimes");

            migrationBuilder.DropTable(
                name: "AniDbGroups");
        }
    }
}
