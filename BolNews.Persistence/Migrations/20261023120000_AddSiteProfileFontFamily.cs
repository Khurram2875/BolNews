using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261023120000_AddSiteProfileFontFamily")]
public partial class AddSiteProfileFontFamily : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FontFamily",
            table: "SiteProfiles",
            type: "varchar(80)",
            maxLength: 80,
            nullable: false,
            defaultValue: "Inter")
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "FontFamily", table: "SiteProfiles");
    }
}
