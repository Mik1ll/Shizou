using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shizou.Migrations
{
    public partial class anidb_tables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AniDbAnimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EpisodeCount = table.Column<int>(type: "INTEGER", nullable: false),
                    AirDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AnimeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Restricted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbAnimes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AniDbGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AniDbEpisodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AniDbAnimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Length = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    EpisodeType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AirDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Hash = table.Column<string>(type: "TEXT", nullable: false),
                    AniDbGroupId = table.Column<int>(type: "INTEGER", nullable: true),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    AudioCodec = table.Column<string>(type: "TEXT", nullable: true),
                    VideoCodec = table.Column<string>(type: "TEXT", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Upadate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WatchedStatus = table.Column<bool>(type: "INTEGER", nullable: false),
                    WatchedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Crc = table.Column<string>(type: "TEXT", nullable: false),
                    Md5 = table.Column<string>(type: "TEXT", nullable: false),
                    Sha1 = table.Column<string>(type: "TEXT", nullable: false),
                    FIleName = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    FileVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Censored = table.Column<bool>(type: "INTEGER", nullable: false),
                    Deprecated = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AniDbFiles_AniDbGroups_AniDbGroupId",
                        column: x => x.AniDbGroupId,
                        principalTable: "AniDbGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AniDbEpisodeAniDbFile",
                columns: table => new
                {
                    AniDbEpisodesId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbFilesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbEpisodeAniDbFile", x => new { x.AniDbEpisodesId, x.AniDbFilesId });
                    table.ForeignKey(
                        name: "FK_AniDbEpisodeAniDbFile_AniDbEpisodes_AniDbEpisodesId",
                        column: x => x.AniDbEpisodesId,
                        principalTable: "AniDbEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AniDbEpisodeAniDbFile_AniDbFiles_AniDbFilesId",
                        column: x => x.AniDbFilesId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AniDbEpisodeAniDbFile_AniDbFilesId",
                table: "AniDbEpisodeAniDbFile",
                column: "AniDbFilesId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbEpisodes_AniDbAnimeId",
                table: "AniDbEpisodes",
                column: "AniDbAnimeId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbFiles_AniDbGroupId",
                table: "AniDbFiles",
                column: "AniDbGroupId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AniDbEpisodeAniDbFile");

            migrationBuilder.DropTable(
                name: "AniDbEpisodes");

            migrationBuilder.DropTable(
                name: "AniDbFiles");

            migrationBuilder.DropTable(
                name: "AniDbAnimes");

            migrationBuilder.DropTable(
                name: "AniDbGroups");
        }
    }
}
