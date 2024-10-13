using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AniDbCharacters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageFilename = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbCharacters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AniDbCreators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ImageFilename = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbCreators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AniDbCredits",
                columns: table => new
                {
                    AniDbAnimeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: true),
                    AniDbCreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    AniDbCharacterId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbCredits", x => new { x.AniDbAnimeId, x.Id });
                    table.ForeignKey(
                        name: "FK_AniDbCredits_AniDbAnimes_AniDbAnimeId",
                        column: x => x.AniDbAnimeId,
                        principalTable: "AniDbAnimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AniDbCredits_AniDbCharacters_AniDbCharacterId",
                        column: x => x.AniDbCharacterId,
                        principalTable: "AniDbCharacters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AniDbCredits_AniDbCreators_AniDbCreatorId",
                        column: x => x.AniDbCreatorId,
                        principalTable: "AniDbCreators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AniDbCredits_AniDbCharacterId",
                table: "AniDbCredits",
                column: "AniDbCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_AniDbCredits_AniDbCreatorId",
                table: "AniDbCredits",
                column: "AniDbCreatorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AniDbCredits");

            migrationBuilder.DropTable(
                name: "AniDbCharacters");

            migrationBuilder.DropTable(
                name: "AniDbCreators");
        }
    }
}
