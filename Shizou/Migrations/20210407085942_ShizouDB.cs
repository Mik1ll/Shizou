using Microsoft.EntityFrameworkCore.Migrations;

namespace Shizou.Migrations
{
    public partial class ShizouDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    QueueType = table.Column<int>(type: "INTEGER", nullable: false),
                    CommandId = table.Column<string>(type: "TEXT", nullable: false),
                    CommandParams = table.Column<string>(type: "TEXT", nullable: false)
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
                    Location = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportFolders", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandRequests");

            migrationBuilder.DropTable(
                name: "ImportFolders");
        }
    }
}
