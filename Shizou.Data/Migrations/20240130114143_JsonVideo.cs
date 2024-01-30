using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class JsonVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.Sql("""
            UPDATE AniDbFiles
            SET Video_Codec = json_object('Codec', Video_Codec, 'Bitrate', Video_BitRate, 'ColorDepth', Video_ColorDepth, 'Height', Video_Height, 'Width', Video_Width);
            """);
            
            migrationBuilder.RenameColumn(
                name: "Video_Codec",
                table: "AniDbFiles",
                newName: "Video");
            
            migrationBuilder.DropColumn(
                name: "Video_BitRate",
                table: "AniDbFiles");

            migrationBuilder.DropColumn(
                name: "Video_ColorDepth",
                table: "AniDbFiles");

            migrationBuilder.DropColumn(
                name: "Video_Height",
                table: "AniDbFiles");

            migrationBuilder.DropColumn(
                name: "Video_Width",
                table: "AniDbFiles");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Video",
                table: "AniDbFiles",
                newName: "Video_Codec");

            migrationBuilder.AddColumn<int>(
                name: "Video_BitRate",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Video_ColorDepth",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Video_Height",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Video_Width",
                table: "AniDbFiles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql("""
            UPDATE AniDbFiles
            SET Video_BitRate    = Video_Codec ->> '$.Bitrate',
                Video_ColorDepth = Video_Codec ->> '$.ColorDepth',
                Video_Width      = Video_Codec ->> '$.Width',
                Video_Height     = Video_Codec ->> '$.Height',
                Video_Codec      = Video_Codec ->> '$.Codec';
            """);
        }
    }
}
