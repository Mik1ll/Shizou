using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class DateOnlyEpisodeAirDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            UPDATE AniDbEpisodes
            SET AirDate = substr(AirDate, 1, 10)
            WHERE length(AirDate) > 10;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
