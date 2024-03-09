using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEpisodeWatchState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpisodeWatchedStates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EpisodeWatchedStates",
                columns: table => new
                {
                    AniDbEpisodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: true),
                    MyListId = table.Column<int>(type: "INTEGER", nullable: true),
                    Watched = table.Column<bool>(type: "INTEGER", nullable: false),
                    WatchedUpdated = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeWatchedStates", x => x.AniDbEpisodeId);
                    table.ForeignKey(
                        name: "FK_EpisodeWatchedStates_AniDbEpisodes_AniDbEpisodeId",
                        column: x => x.AniDbEpisodeId,
                        principalTable: "AniDbEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeWatchedStates_MyListId",
                table: "EpisodeWatchedStates",
                column: "MyListId");
        }
    }
}
