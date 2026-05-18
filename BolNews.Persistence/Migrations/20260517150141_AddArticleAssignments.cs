using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FactCheckerUserId",
                table: "Articles",
                type: "varchar(95)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerUserId",
                table: "Articles",
                type: "varchar(95)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_FactCheckerUserId",
                table: "Articles",
                column: "FactCheckerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReviewerUserId",
                table: "Articles",
                column: "ReviewerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_AspNetUsers_FactCheckerUserId",
                table: "Articles",
                column: "FactCheckerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_AspNetUsers_ReviewerUserId",
                table: "Articles",
                column: "ReviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_AspNetUsers_FactCheckerUserId",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_AspNetUsers_ReviewerUserId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_FactCheckerUserId",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ReviewerUserId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "FactCheckerUserId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ReviewerUserId",
                table: "Articles");
        }
    }
}
