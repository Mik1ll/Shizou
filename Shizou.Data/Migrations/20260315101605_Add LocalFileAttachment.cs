using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFileAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocalFileAttachments",
                columns: table => new
                {
                    LocalFileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    Hash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalFileAttachments", x => new { x.LocalFileId, x.Filename });
                    table.ForeignKey(
                        name: "FK_LocalFileAttachments_LocalFiles_LocalFileId",
                        column: x => x.LocalFileId,
                        principalTable: "LocalFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocalFileAttachments_Hash",
                table: "LocalFileAttachments",
                column: "Hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocalFileAttachments");
        }
    }
}
