using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateArticleImagesURl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FeaturedImageUrl",
                table: "Articles",
                newName: "FeaturedImageXl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FeaturedImageXl",
                table: "Articles",
                newName: "FeaturedImageUrl");
        }
    }
}
