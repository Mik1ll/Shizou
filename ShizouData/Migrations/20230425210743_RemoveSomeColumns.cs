using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShizouData.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSomeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HighestEpisode",
                table: "AniDbAnimes");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AniDbUpdated",
                table: "AniDbAnimes",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "AniDbUpdated",
                table: "AniDbAnimes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HighestEpisode",
                table: "AniDbAnimes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
