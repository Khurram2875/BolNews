using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261111000000_AddSiteProfileLanguage")]
public partial class AddSiteProfileLanguage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LanguageCode",
            table: "SiteProfiles",
            type: "varchar(8)",
            maxLength: 8,
            nullable: false,
            defaultValue: "en")
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "LanguageCode", table: "SiteProfiles");
    }
}
