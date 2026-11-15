using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261115020000_AddPublisherHomeLayout")]
public partial class AddPublisherHomeLayout : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "HomeLayoutJson",
            table: "SiteProfiles",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "HomeLayoutJson", table: "SiteProfiles");
    }
}
