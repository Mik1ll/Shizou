using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class JsonAudioSubtitles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Audio",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitles",
                table: "AniDbFiles",
                type: "TEXT",
                nullable: true);
            
            migrationBuilder.Sql("""
            UPDATE AniDbFiles
            SET Audio = (SELECT json_group_array(json_object('Language', AniDbAudio.Language, 'Codec', AniDbAudio.Codec, 'Bitrate', AniDbAudio.Bitrate))
                         FROM AniDbAudio
                         WHERE AniDbAudio.AniDbFileId = AniDbFiles.Id
                         GROUP BY AniDbAudio.AniDbFileId),
                Subtitles = (SELECT json_group_array(json_object('Language', AniDbSubtitles.Language))
                             FROM AniDbSubtitles
                             WHERE AniDbSubtitles.AniDbFileId = AniDbFiles.Id
                             GROUP BY AniDbSubtitles.AniDbFileId);
            UPDATE AniDbFiles
            SET Audio = '[]'
            WHERE Audio IS NULL;
            UPDATE AniDbFiles
            SET Subtitles = '[]'
            WHERE Subtitles IS NULL;
            """);

            migrationBuilder.DropTable(
                name: "AniDbAudio");

            migrationBuilder.DropTable(
                name: "AniDbSubtitles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.CreateTable(
                name: "AniDbAudio",
                columns: table => new
                {
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Bitrate = table.Column<int>(type: "INTEGER", nullable: false),
                    Codec = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbAudio", x => new { x.AniDbFileId, x.Id });
                    table.ForeignKey(
                        name: "FK_AniDbAudio_AniDbFiles_AniDbFileId",
                        column: x => x.AniDbFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AniDbSubtitles",
                columns: table => new
                {
                    AniDbFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AniDbSubtitles", x => new { x.AniDbFileId, x.Id });
                    table.ForeignKey(
                        name: "FK_AniDbSubtitles_AniDbFiles_AniDbFileId",
                        column: x => x.AniDbFileId,
                        principalTable: "AniDbFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
            INSERT INTO AniDbSubtitles (Id, AniDbFileId, Language)
            SELECT key + 1, AniDbFiles.Id, json_extract(value, '$.Language')
            FROM AniDbFiles, json_each(AniDbFiles.Subtitles);
            INSERT INTO AniDbAudio (Id, AniDbFileId, Language, Codec, Bitrate)
            SELECT key + 1, AniDbFiles.Id, json_extract(value, '$.Language'), json_extract(value, '$.Codec'), json_extract(value, '$.Bitrate')
            FROM AniDbFiles, json_each(AniDbFiles.Audio);
            """);

            migrationBuilder.DropColumn(
                name: "Audio",
                table: "AniDbFiles");

            migrationBuilder.DropColumn(
                name: "Subtitles",
                table: "AniDbFiles");
        }
    }
}
