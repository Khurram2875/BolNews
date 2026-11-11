using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261104000000_AddSiteSettings")]
public partial class AddSiteSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiteSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Value = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UpdatedBy = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table => { table.PrimaryKey("PK_SiteSettings", x => x.Id); })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_SiteSettings_Key", table: "SiteSettings", column: "Key", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiteSettings");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The current model snapshot is maintained in AppDbContextModelSnapshot.cs.
    }
}
