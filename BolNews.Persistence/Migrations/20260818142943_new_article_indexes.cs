using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class new_article_indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Articles_Published_Deleted_Category_PublishedAt",
                table: "Articles",
                columns: new[] { "IsPublished", "IsDeleted", "CategoryId", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Published_Deleted_OverallScore_PublishedAt",
                table: "Articles",
                columns: new[] { "IsPublished", "IsDeleted", "OverallScore", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_Published_Deleted_Category_PublishedAt",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_Published_Deleted_OverallScore_PublishedAt",
                table: "Articles");
        }
    }
}
