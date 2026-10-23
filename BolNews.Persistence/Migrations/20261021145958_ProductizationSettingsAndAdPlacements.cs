using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BolNews.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductizationSettingsAndAdPlacements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdPlacements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZoneKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ElementId = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProviderUnit = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContainerClass = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ParagraphIndex = table.Column<int>(type: "int", nullable: true),
                    InsertionMode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdPlacements", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SiteProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SiteName = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShortName = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tagline = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeaderLogoUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FooterLogoUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FaviconUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrimaryColor = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccentColor = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThemeName = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LiveStreamUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CopyrightText = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultMetaTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultMetaDescription = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdScriptUrl = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WhatsAppUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FacebookUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    XUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YouTubeUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InstagramUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TikTokUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UrduEditionUrl = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OtherEditionLabel = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FooterColumnOneLinks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FooterColumnTwoLinks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FooterColumnThreeLinks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AboutContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdvertiseContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlogsContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BrandedContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EditorialPolicyContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PrivacyPolicyContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TermsOfServiceContent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteProfiles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AdPlacements_Key",
                table: "AdPlacements",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdPlacements_ZoneKey_IsEnabled_SortOrder",
                table: "AdPlacements",
                columns: new[] { "ZoneKey", "IsEnabled", "SortOrder" });

            migrationBuilder.InsertData(
                table: "SiteProfiles",
                columns: new[] { "Id", "SiteName", "ShortName", "Tagline", "HeaderLogoUrl", "FooterLogoUrl", "FaviconUrl", "PrimaryColor", "AccentColor", "ThemeName", "LiveStreamUrl", "CopyrightText", "DefaultMetaTitle", "DefaultMetaDescription", "AdScriptUrl", "WhatsAppUrl", "FacebookUrl", "XUrl", "YouTubeUrl", "InstagramUrl", "TikTokUrl", "UrduEditionUrl", "OtherEditionLabel", "FooterColumnOneLinks", "FooterColumnTwoLinks", "FooterColumnThreeLinks", "UpdatedAt" },
                values: new object[] { 1, "Bol News", "BOL News", "Latest News, Politics, Sports and Entertainment", "/images/logo-black.png", "/images/logo-black.png", "/bol_network.ico", "#d30b12", "#d30b12", "default", "/live", "BOL Media Network. All rights reserved.", "Bol News - Latest News, Politics, Sports and Entertainment", "Latest news from Bol News", "https://avads.live/s/av-bolnews.js", "https://whatsapp.com/channel/0029Vb7ZANWGJP8NYx9M4g0I", "https://www.facebook.com/BOLNETWORK/", "https://twitter.com/BOLNETWORK", "https://www.youtube.com/@BOLNewsofficial", "https://www.instagram.com/bolnetwork/", "https://www.tiktok.com/@bolnews", "https://urdu.bolnews.com", "Urdu", "Latest News|/category/latest-news\nHealth|/category/health\nShowbiz|/category/showbiz\nTrending|/category/trending\nViral|/category/viral", "About Us|/about-us\nContact Us|/contact-us\nAdvertise With Us|/advertise\nUrdu|https://urdu.bolnews.com\nLive|/live", "Branded Content|/branded-content\nPrivacy Policy|/privacy-policy\nTerms Of Service|/terms-of-service\nEditorial Policy|/editorial-policy\nBlog|/blogs", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) });

            // Preserve current BOL ad locations while allowing each installation to change them later.
            migrationBuilder.InsertData(
                table: "AdPlacements",
                columns: new[] { "Key", "DisplayName", "ZoneKey", "ElementId", "ProviderUnit", "ContainerClass", "IsEnabled", "SortOrder", "ParagraphIndex", "InsertionMode", "UpdatedAt" },
                values: new object[,]
                {
                    { "site-top-leaderboard", "Top leaderboard", "site.top", "Bolnews_Top_Leaderboard_ATF", "top-leaderboard-atf", "bn-ad-slot-leaderboard", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "site-bottom-banner", "Bottom banner", "site.bottom", "Bolnews_Bottom_BTF", "bottom-btf", "bn-ad-slot-banner", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "home-sidebar-top", "Homepage sidebar top", "home.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "home-sidebar-bottom", "Homepage sidebar bottom", "home.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "home-content-incontent", "Homepage content ad", "home.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "article-sidebar-top", "Article sidebar top", "article.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "article-sidebar-bottom", "Article sidebar bottom", "article.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "article-inline-incontent", "Article body ad", "article.inline", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", true, 0, 2, "afterParagraph", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "category-sidebar-top", "Category sidebar top", "category.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "category-sidebar-bottom", "Category sidebar bottom", "category.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "category-content-incontent", "Category content ad", "category.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "tag-sidebar-top", "Tag sidebar top", "tag.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "tag-sidebar-bottom", "Tag sidebar bottom", "tag.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "tag-content-incontent", "Tag content ad", "tag.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "search-sidebar-top", "Search sidebar top", "search.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "search-sidebar-bottom", "Search sidebar bottom", "search.sidebar.bottom", "Bolnews_Sidebar_Bottom_BTF", "sidebar-bottom-btf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "search-content-incontent", "Search results ad", "search.content", "Bolnews_Incontent_Lazy", "incontent", "bn-ad-slot-incontent", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) },
                    { "video-sidebar-top", "Video sidebar top", "video.sidebar.top", "Bolnews_Sidebar_Top_ATF", "sidebar-top-atf", "bn-ad-slot-sidebar", true, 0, null, "zone", new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdPlacements");

            migrationBuilder.DropTable(
                name: "SiteProfiles");
        }
    }
}
