using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderWatched : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Watched",
                table: "ImportFolders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Watched",
                table: "ImportFolders");
        }
    }
}
