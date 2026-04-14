using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MariaDB_InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterDatabase()
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "Authors",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //        Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Bio = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ProfileImageUrl = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Authors", x => x.Id);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "Categories",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //        Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Slug = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        ParentCategoryId = table.Column<int>(type: "int", nullable: true),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Categories", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Categories_Categories_ParentCategoryId",
            //            column: x => x.ParentCategoryId,
            //            principalTable: "Categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateTable(
            //    name: "Articles",
            //    columns: table => new
            //    {
            //        Id = table.Column<int>(type: "int", nullable: false)
            //            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
            //        Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Slug = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Summary = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        Content = table.Column<string>(type: "longtext", nullable: false)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FeaturedImageUrl = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FeaturedImageThumb = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FeaturedImageMedium = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        FeaturedImageLarge = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        CategoryId = table.Column<int>(type: "int", nullable: false),
            //        AuthorId = table.Column<int>(type: "int", nullable: false),
            //        IsPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
            //        PublishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        ViewCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
            //        CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            //        CreatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            //        UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
            //            .Annotation("MySql:CharSet", "utf8mb4"),
            //        IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
            //    },
            //    constraints: table =>
            //    {
            //        table.PrimaryKey("PK_Articles", x => x.Id);
            //        table.ForeignKey(
            //            name: "FK_Articles_Authors_AuthorId",
            //            column: x => x.AuthorId,
            //            principalTable: "Authors",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //        table.ForeignKey(
            //            name: "FK_Articles_Categories_CategoryId",
            //            column: x => x.CategoryId,
            //            principalTable: "Categories",
            //            principalColumn: "Id",
            //            onDelete: ReferentialAction.Restrict);
            //    })
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Articles_AuthorId",
            //    table: "Articles",
            //    column: "AuthorId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Articles_CategoryId",
            //    table: "Articles",
            //    column: "CategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Articles_IsPublished",
            //    table: "Articles",
            //    column: "IsPublished");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Articles_PublishedAt",
            //    table: "Articles",
            //    column: "PublishedAt");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Articles_Slug",
            //    table: "Articles",
            //    column: "Slug",
            //    unique: true);

            //migrationBuilder.CreateIndex(
            //    name: "IX_Categories_ParentCategoryId",
            //    table: "Categories",
            //    column: "ParentCategoryId");

            //migrationBuilder.CreateIndex(
            //    name: "IX_Categories_Slug",
            //    table: "Categories",
            //    column: "Slug",
            //    unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Authors");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
