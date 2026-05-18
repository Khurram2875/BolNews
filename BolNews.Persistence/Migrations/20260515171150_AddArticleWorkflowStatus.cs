using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleWorkflowStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "Articles");
        }
    }
}
