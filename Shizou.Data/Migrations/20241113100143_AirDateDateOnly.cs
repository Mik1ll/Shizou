using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class AirDateDateOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            UPDATE AniDbAnimes
            SET AirDate = CASE
                              WHEN length(AirDate) = 7 THEN AirDate || '-01'
                              WHEN length(AirDate) = 4 THEN AirDate || '-01-01'
                              ELSE AirDate
                END
            WHERE length(AirDate) = 7 OR length(AirDate) = 4;
            """);
            migrationBuilder.Sql("""
            UPDATE AniDbAnimes
            SET EndDate = CASE
                              WHEN length(EndDate) = 7 THEN date(EndDate || '-01', '+1 month', '-1 day')
                              WHEN length(EndDate) = 4 THEN date(EndDate || '-01-01', '+1 year', '-1 day')
                              ELSE EndDate
                END
            WHERE length(EndDate) = 7 OR length(EndDate) = 4;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
