using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurableLivePlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LiveThumbnailUrl",
                table: "SiteProfiles",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LiveVideoEmbedUrl",
                table: "SiteProfiles",
                type: "varchar(1024)",
                maxLength: 1024,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("UPDATE SiteProfiles SET LiveVideoEmbedUrl = 'https://www.youtube.com/embed/9N6SWn1BQpA?rel=0&modestbranding=1', LiveThumbnailUrl = 'https://img.youtube.com/vi/9N6SWn1BQpA/maxresdefault.jpg' WHERE Id = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiveThumbnailUrl",
                table: "SiteProfiles");

            migrationBuilder.DropColumn(
                name: "LiveVideoEmbedUrl",
                table: "SiteProfiles");
        }
    }
}
