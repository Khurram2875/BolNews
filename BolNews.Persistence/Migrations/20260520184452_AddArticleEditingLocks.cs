using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleEditingLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<DateTime>(
            //    name: "LockedAt",
            //    table: "Articles",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "LockedByUserId",
            //    table: "Articles",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "Articles");
        }
    }
}
