using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;

#nullable disable

namespace Shizou.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAniDbQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE AniDbEpisodes
                SET TitleEnglish = replace(TitleEnglish, '`', ''''),
                    TitleTranscription = replace(TitleTranscription, '`', ''''),
                    TitleOriginal = replace(TitleOriginal, '`', ''''),
                    Summary = replace(Summary, '`', '''');
                """);
            migrationBuilder.Sql("""
                UPDATE AniDbAnimes
                SET TitleEngish = replace(TitleEngish, '`', ''''),
                    TitleTranscription = replace(TitleTranscription, '`', ''''),
                    TitleOriginal = replace(TitleOriginal, '`', ''''),
                    Description = replace(Description, '`', ''''),
                    Tags = replace(Tags, '`', '''');
                """);
            migrationBuilder.Sql("""
                UPDATE AniDbCharacters
                SET Name = replace(Name, '`', '''');
                """);
            migrationBuilder.Sql("""
                 UPDATE AniDbCreators
                 SET Name = replace(Name, '`', '''');
                 """);
            migrationBuilder.Sql("""
                UPDATE AniDbGroups
                SET Name = replace(Name, '`', ''''),
                    ShortName = replace(ShortName, '`', '''');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
