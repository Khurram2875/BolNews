using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261030000000_AddLiveEventElectionCards")]
public partial class AddLiveEventElectionCards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "EventType",
            table: "LiveEvents",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "ElectionHeader",
            table: "LiveEvents",
            type: "varchar(100)",
            maxLength: 100,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "RaceTitle",
            table: "LiveEvents",
            type: "varchar(100)",
            maxLength: 100,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "RaceSubtitle",
            table: "LiveEvents",
            type: "varchar(100)",
            maxLength: 100,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "LiveEventCandidates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                LiveEventId = table.Column<int>(type: "int", nullable: false),
                Party = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                Votes = table.Column<int>(type: "int", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LiveEventCandidates", x => x.Id);
                table.ForeignKey("FK_LiveEventCandidates_LiveEvents_LiveEventId", x => x.LiveEventId, "LiveEvents", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_LiveEventCandidates_LiveEventId_SortOrder", table: "LiveEventCandidates", columns: new[] { "LiveEventId", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LiveEventCandidates");
        migrationBuilder.DropColumn(name: "RaceSubtitle", table: "LiveEvents");
        migrationBuilder.DropColumn(name: "RaceTitle", table: "LiveEvents");
        migrationBuilder.DropColumn(name: "ElectionHeader", table: "LiveEvents");
        migrationBuilder.DropColumn(name: "EventType", table: "LiveEvents");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The current model snapshot is maintained in AppDbContextModelSnapshot.cs.
    }
}
