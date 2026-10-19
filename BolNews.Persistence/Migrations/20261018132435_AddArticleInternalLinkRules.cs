using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleInternalLinkRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArticleInternalLinkRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceArticleId = table.Column<int>(type: "int", nullable: false),
                    TargetArticleId = table.Column<int>(type: "int", nullable: true),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    AnchorText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetUrl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleInternalLinkRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleInternalLinkRules_Articles_SourceArticleId",
                        column: x => x.SourceArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleInternalLinkRules_Articles_TargetArticleId",
                        column: x => x.TargetArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleInternalLinkRules_SourceArticleId",
                table: "ArticleInternalLinkRules",
                column: "SourceArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleInternalLinkRules_TargetArticleId",
                table: "ArticleInternalLinkRules",
                column: "TargetArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleInternalLinkRules");

        }
    }
}
