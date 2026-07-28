using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeaturedMediaId",
                table: "Articles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MediumUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LargeUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AltText = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Caption = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Credit = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OriginalFileName = table.Column<string>(type: "varchar(260)", maxLength: 260, nullable: true)
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
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MediaAssetTags",
                columns: table => new
                {
                    MediaAssetId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssetTags", x => new { x.MediaAssetId, x.TagId });
                    table.ForeignKey(
                        name: "FK_MediaAssetTags_MediaAssets_MediaAssetId",
                        column: x => x.MediaAssetId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaAssetTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_FeaturedMediaId",
                table: "Articles",
                column: "FeaturedMediaId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Caption",
                table: "MediaAssets",
                column: "Caption");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_MediaType_CreatedAt",
                table: "MediaAssets",
                columns: new[] { "MediaType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssetTags_TagId_MediaAssetId",
                table: "MediaAssetTags",
                columns: new[] { "TagId", "MediaAssetId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_MediaAssets_FeaturedMediaId",
                table: "Articles",
                column: "FeaturedMediaId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Preserve the existing article image URLs and promote each featured image
            // into a reusable library asset. Article image folders remain untouched.
            migrationBuilder.Sql(@"
                INSERT INTO `MediaAssets` (`MediaType`, `Url`, `ThumbnailUrl`, `MediumUrl`, `LargeUrl`, `AltText`, `Caption`, `Credit`, `CreatedAt`, `CreatedBy`, `IsDeleted`)
                SELECT 1, a.`FeaturedImageXl`, a.`FeaturedImageThumb`, a.`FeaturedImageMedium`, a.`FeaturedImageLarge`, m.`AltText`, m.`Caption`, m.`Credit`, a.`CreatedAt`, a.`CreatedBy`, 0
                FROM `Articles` a
                LEFT JOIN `FeaturedImageMetadata` m ON m.`ArticleId` = a.`Id`
                WHERE a.`IsDeleted` = 0 AND a.`FeaturedImageXl` IS NOT NULL AND a.`FeaturedImageXl` <> '';" );

            migrationBuilder.Sql(@"
                UPDATE `Articles` a
                INNER JOIN `MediaAssets` m ON m.`Url` = a.`FeaturedImageXl` AND m.`MediaType` = 1
                SET a.`FeaturedMediaId` = m.`Id`
                WHERE a.`IsDeleted` = 0 AND a.`FeaturedImageXl` IS NOT NULL AND a.`FeaturedImageXl` <> '';" );

            migrationBuilder.Sql(@"
                INSERT IGNORE INTO `MediaAssetTags` (`MediaAssetId`, `TagId`)
                SELECT ma.`Id`, fit.`TagId`
                FROM `FeaturedImageTags` fit
                INNER JOIN `FeaturedImageMetadata` fim ON fim.`Id` = fit.`FeaturedImageMetadataId`
                INNER JOIN `Articles` a ON a.`Id` = fim.`ArticleId`
                INNER JOIN `MediaAssets` ma ON ma.`Url` = a.`FeaturedImageXl` AND ma.`MediaType` = 1
                WHERE a.`IsDeleted` = 0;" );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Articles_MediaAssets_FeaturedMediaId",
                table: "Articles");

            migrationBuilder.DropTable(
                name: "MediaAssetTags");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropIndex(
                name: "IX_Articles_FeaturedMediaId",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "FeaturedMediaId",
                table: "Articles");
        }
    }
}
