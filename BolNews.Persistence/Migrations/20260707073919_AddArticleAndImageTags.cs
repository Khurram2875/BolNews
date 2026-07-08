using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleAndImageTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeaturedImageMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    AltText = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Caption = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Credit = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedImageMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeaturedImageMetadata_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NormalizedName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Slug = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ArticleTags",
                columns: table => new
                {
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTags", x => new { x.ArticleId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ArticleTags_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FeaturedImageTags",
                columns: table => new
                {
                    FeaturedImageMetadataId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeaturedImageTags", x => new { x.FeaturedImageMetadataId, x.TagId });
                    table.ForeignKey(
                        name: "FK_FeaturedImageTags_FeaturedImageMetadata_FeaturedImageMetadat~",
                        column: x => x.FeaturedImageMetadataId,
                        principalTable: "FeaturedImageMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeaturedImageTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_TagId",
                table: "ArticleTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleTags_TagId_ArticleId",
                table: "ArticleTags",
                columns: new[] { "TagId", "ArticleId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedImageMetadata_ArticleId",
                table: "FeaturedImageMetadata",
                column: "ArticleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedImageTags_TagId",
                table: "FeaturedImageTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_FeaturedImageTags_TagId_FeaturedImageMetadataId",
                table: "FeaturedImageTags",
                columns: new[] { "TagId", "FeaturedImageMetadataId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Slug",
                table: "Tags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: "FeaturedImageTags");

            migrationBuilder.DropTable(
                name: "FeaturedImageMetadata");

            migrationBuilder.DropTable(
                name: "Tags");
        }
    }
}
