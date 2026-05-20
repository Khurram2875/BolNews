using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowSlaTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ApprovedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "FactCheckStartedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ReviewStartedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "SubmittedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "FactCheckStartedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ReviewStartedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Articles");
        }
    }
}
