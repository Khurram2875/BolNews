using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using BolNews.Persistence.Context;

#nullable disable

namespace BolNews.Infrastructure.Migrations;

[Migration("20261002000000_AddMediaContentHash")]
[DbContext(typeof(AppDbContext))]
public partial class AddMediaContentHash : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContentHash",
            table: "MediaAssets",
            type: "varchar(64)",
            maxLength: 64,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_MediaAssets_MediaType_ContentHash",
            table: "MediaAssets",
            columns: new[] { "MediaType", "ContentHash" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_MediaAssets_MediaType_ContentHash",
            table: "MediaAssets");

        migrationBuilder.DropColumn(
            name: "ContentHash",
            table: "MediaAssets");
    }
}
