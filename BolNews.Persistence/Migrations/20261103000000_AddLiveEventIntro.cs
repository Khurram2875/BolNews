using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261103000000_AddLiveEventIntro")]
public partial class AddLiveEventIntro : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IntroHtml",
            table: "LiveEvents",
            type: "longtext",
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IntroHtml", table: "LiveEvents");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The current model snapshot is maintained in AppDbContextModelSnapshot.cs.
    }
}
