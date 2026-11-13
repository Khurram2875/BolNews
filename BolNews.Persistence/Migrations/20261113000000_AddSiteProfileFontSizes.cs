using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261113000000_AddSiteProfileFontSizes")]
public partial class AddSiteProfileFontSizes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(name: "BaseFontSizePx", table: "SiteProfiles", type: "int", nullable: false, defaultValue: 16);
        migrationBuilder.AddColumn<int>(name: "ArticleFontSizePx", table: "SiteProfiles", type: "int", nullable: false, defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ArticleFontSizePx", table: "SiteProfiles");
        migrationBuilder.DropColumn(name: "BaseFontSizePx", table: "SiteProfiles");
    }
}
