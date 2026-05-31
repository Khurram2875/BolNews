using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledPublishing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "EmbargoUntil",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "ScheduledPublishAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbargoUntil",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ScheduledPublishAt",
                table: "Articles");
        }
    }
}
