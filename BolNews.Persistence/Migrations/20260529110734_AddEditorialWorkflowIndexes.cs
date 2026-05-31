using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEditorialWorkflowIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Articles_EmbargoUntil",
                table: "Articles",
                column: "EmbargoUntil");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_IsPublished_PublishedAt",
                table: "Articles",
                columns: new[] { "IsPublished", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ScheduledPublishAt",
                table: "Articles",
                column: "ScheduledPublishAt");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_WorkflowStatus",
                table: "Articles",
                column: "WorkflowStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_EmbargoUntil",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_IsPublished_PublishedAt",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_ScheduledPublishAt",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_WorkflowStatus",
                table: "Articles");
        }
    }
}
