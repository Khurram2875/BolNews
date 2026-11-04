using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace BolNews.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261105000000_AddPolls")]
public partial class AddPolls : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Polls",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                Title = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                PollType = table.Column<int>(type: "int", nullable: false),
                FeaturedMediaId = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                EndsAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedBy = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true).Annotation("MySql:CharSet", "utf8mb4"),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                UpdatedBy = table.Column<string>(type: "varchar(450)", maxLength: 450, nullable: true).Annotation("MySql:CharSet", "utf8mb4")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Polls", x => x.Id);
                table.ForeignKey("FK_Polls_MediaAssets_FeaturedMediaId", x => x.FeaturedMediaId, "MediaAssets", "Id", onDelete: ReferentialAction.Restrict);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "PollOptions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                PollId = table.Column<int>(type: "int", nullable: false),
                Text = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PollOptions", x => x.Id);
                table.ForeignKey("FK_PollOptions_Polls_PollId", x => x.PollId, "Polls", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "PollVotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                PollId = table.Column<int>(type: "int", nullable: false),
                OptionId = table.Column<int>(type: "int", nullable: false),
                VoterHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                IpHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false).Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PollVotes", x => x.Id);
                table.ForeignKey("FK_PollVotes_Polls_PollId", x => x.PollId, "Polls", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_PollVotes_PollOptions_OptionId", x => x.OptionId, "PollOptions", "Id", onDelete: ReferentialAction.Cascade);
            }).Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(name: "IX_Polls_FeaturedMediaId", table: "Polls", column: "FeaturedMediaId");
        migrationBuilder.CreateIndex(name: "IX_Polls_IsActive_EndsAt", table: "Polls", columns: new[] { "IsActive", "EndsAt" });
        migrationBuilder.CreateIndex(name: "IX_PollOptions_PollId_SortOrder", table: "PollOptions", columns: new[] { "PollId", "SortOrder" });
        migrationBuilder.CreateIndex(name: "IX_PollVotes_PollId_VoterHash", table: "PollVotes", columns: new[] { "PollId", "VoterHash" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_PollVotes_PollId_OptionId", table: "PollVotes", columns: new[] { "PollId", "OptionId" });
        migrationBuilder.CreateIndex(name: "IX_PollVotes_PollId_IpHash", table: "PollVotes", columns: new[] { "PollId", "IpHash" });
        migrationBuilder.CreateIndex(name: "IX_PollVotes_OptionId", table: "PollVotes", column: "OptionId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PollVotes");
        migrationBuilder.DropTable(name: "PollOptions");
        migrationBuilder.DropTable(name: "Polls");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The current model snapshot is maintained in AppDbContextModelSnapshot.cs.
    }
}
