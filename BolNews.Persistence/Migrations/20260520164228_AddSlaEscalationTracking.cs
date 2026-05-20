using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSlaEscalationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "FactCheckEscalatedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "PublishEscalatedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ReviewEscalatedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FactCheckEscalatedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "PublishEscalatedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ReviewEscalatedAt",
                table: "Articles");
        }
    }
}
