using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleScoringFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.AddColumn<decimal>(
                name: "CredibilityScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EditorialScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EngagementScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FreshnessScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastScoreCalculatedAt",
                table: "Articles",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OverallScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PopularityScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeoScore",
                table: "Articles",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CategoryId_OverallScore",
                table: "Articles",
                columns: new[] { "CategoryId", "OverallScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_OverallScore",
                table: "Articles",
                column: "OverallScore");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Articles_CategoryId_OverallScore",
                table: "Articles");

            migrationBuilder.DropIndex(
                name: "IX_Articles_OverallScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "CredibilityScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "EditorialScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "EngagementScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "FreshnessScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "LastScoreCalculatedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "OverallScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PopularityScore",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "SeoScore",
                table: "Articles");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_CategoryId",
                table: "Articles",
                column: "CategoryId");
        }
    }
}
