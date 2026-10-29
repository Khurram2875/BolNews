using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261029000000_AddLiveEvents")]
public partial class AddLiveEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LiveEvents",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Slug = table.Column<string>(type: "varchar(191)", maxLength: 191, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                FeaturedMediaId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                IsPinned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                ShowLiveBadge = table.Column<bool>(type: "tinyint(1)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedBy = table.Column<string>(type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                UpdatedBy = table.Column<string>(type: "longtext", nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LiveEvents", x => x.Id);
                table.ForeignKey("FK_LiveEvents_MediaAssets_FeaturedMediaId", x => x.FeaturedMediaId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "LiveEventUpdates",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                LiveEventId = table.Column<int>(type: "int", nullable: false),
                Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                Summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Details = table.Column<string>(type: "longtext", nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Source = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                UpdatedBy = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: false).Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LiveEventUpdates", x => x.Id);
                table.ForeignKey("FK_LiveEventUpdates_LiveEvents_LiveEventId", x => x.LiveEventId, "LiveEvents", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "LiveEventArticles",
            columns: table => new
            {
                LiveEventId = table.Column<int>(type: "int", nullable: false),
                ArticleId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LiveEventArticles", x => new { x.LiveEventId, x.ArticleId });
                table.ForeignKey("FK_LiveEventArticles_Articles_ArticleId", x => x.ArticleId, "Articles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_LiveEventArticles_LiveEvents_LiveEventId", x => x.LiveEventId, "LiveEvents", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_LiveEvents_FeaturedMediaId", table: "LiveEvents", column: "FeaturedMediaId");
        migrationBuilder.CreateIndex(name: "IX_LiveEvents_IsActive_IsPinned_CreatedAt", table: "LiveEvents", columns: new[] { "IsActive", "IsPinned", "CreatedAt" });
        migrationBuilder.CreateIndex(name: "IX_LiveEvents_Slug", table: "LiveEvents", column: "Slug", unique: true);
        migrationBuilder.CreateIndex(name: "IX_LiveEventUpdates_LiveEventId_Timestamp", table: "LiveEventUpdates", columns: new[] { "LiveEventId", "Timestamp" });
        migrationBuilder.CreateIndex(name: "IX_LiveEventArticles_ArticleId", table: "LiveEventArticles", column: "ArticleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LiveEventArticles");
        migrationBuilder.DropTable(name: "LiveEventUpdates");
        migrationBuilder.DropTable(name: "LiveEvents");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The current model snapshot is maintained in AppDbContextModelSnapshot.cs.
    }
}
