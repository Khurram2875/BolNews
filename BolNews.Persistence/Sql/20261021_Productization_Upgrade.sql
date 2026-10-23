START TRANSACTION;

CREATE TABLE `AdPlacements` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Key` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `DisplayName` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `ZoneKey` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `ElementId` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderUnit` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `ContainerClass` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `IsEnabled` tinyint(1) NOT NULL,
    `SortOrder` int NOT NULL,
    `ParagraphIndex` int NULL,
    `InsertionMode` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_AdPlacements` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `SiteProfiles` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SiteName` varchar(160) CHARACTER SET utf8mb4 NOT NULL,
    `ShortName` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `Tagline` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `HeaderLogoUrl` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `FooterLogoUrl` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `FaviconUrl` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `PrimaryColor` varchar(7) CHARACTER SET utf8mb4 NOT NULL,
    `AccentColor` varchar(7) CHARACTER SET utf8mb4 NOT NULL,
    `ThemeName` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `LiveStreamUrl` varchar(512) CHARACTER SET utf8mb4 NOT NULL,
    `CopyrightText` varchar(300) CHARACTER SET utf8mb4 NOT NULL,
    `DefaultMetaTitle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DefaultMetaDescription` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `AdScriptUrl` varchar(1024) CHARACTER SET utf8mb4 NULL,
    `WhatsAppUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `FacebookUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `XUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `YouTubeUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `InstagramUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `TikTokUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `UrduEditionUrl` varchar(512) CHARACTER SET utf8mb4 NULL,
    `OtherEditionLabel` varchar(80) CHARACTER SET utf8mb4 NOT NULL,
    `FooterColumnOneLinks` longtext CHARACTER SET utf8mb4 NULL,
    `FooterColumnTwoLinks` longtext CHARACTER SET utf8mb4 NULL,
    `FooterColumnThreeLinks` longtext CHARACTER SET utf8mb4 NULL,
    `AboutContent` longtext CHARACTER SET utf8mb4 NULL,
    `ContactContent` longtext CHARACTER SET utf8mb4 NULL,
    `AdvertiseContent` longtext CHARACTER SET utf8mb4 NULL,
    `BlogsContent` longtext CHARACTER SET utf8mb4 NULL,
    `BrandedContent` longtext CHARACTER SET utf8mb4 NULL,
    `EditorialPolicyContent` longtext CHARACTER SET utf8mb4 NULL,
    `PrivacyPolicyContent` longtext CHARACTER SET utf8mb4 NULL,
    `TermsOfServiceContent` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_SiteProfiles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_AdPlacements_Key` ON `AdPlacements` (`Key`);

CREATE INDEX `IX_AdPlacements_ZoneKey_IsEnabled_SortOrder` ON `AdPlacements` (`ZoneKey`, `IsEnabled`, `SortOrder`);

INSERT INTO `SiteProfiles` (`Id`, `SiteName`, `ShortName`, `Tagline`, `HeaderLogoUrl`, `FooterLogoUrl`, `FaviconUrl`, `PrimaryColor`, `AccentColor`, `ThemeName`, `LiveStreamUrl`, `CopyrightText`, `DefaultMetaTitle`, `DefaultMetaDescription`, `AdScriptUrl`, `WhatsAppUrl`, `FacebookUrl`, `XUrl`, `YouTubeUrl`, `InstagramUrl`, `TikTokUrl`, `UrduEditionUrl`, `OtherEditionLabel`, `FooterColumnOneLinks`, `FooterColumnTwoLinks`, `FooterColumnThreeLinks`, `UpdatedAt`)
VALUES (1, 'Bol News', 'BOL News', 'Latest News, Politics, Sports and Entertainment', '/images/logo-black.png', '/images/logo-black.png', '/bol_network.ico', '#d30b12', '#d30b12', 'default', '/live', 'BOL Media Network. All rights reserved.', 'Bol News - Latest News, Politics, Sports and Entertainment', 'Latest news from Bol News', 'https://avads.live/s/av-bolnews.js', 'https://whatsapp.com/channel/0029Vb7ZANWGJP8NYx9M4g0I', 'https://www.facebook.com/BOLNETWORK/', 'https://twitter.com/BOLNETWORK', 'https://www.youtube.com/@BOLNewsofficial', 'https://www.instagram.com/bolnetwork/', 'https://www.tiktok.com/@bolnews', 'https://urdu.bolnews.com', 'Urdu', CONCAT('Latest News|/category/latest-news', CHAR(10), 'Health|/category/health', CHAR(10), 'Showbiz|/category/showbiz', CHAR(10), 'Trending|/category/trending', CHAR(10), 'Viral|/category/viral'), CONCAT('About Us|/about-us', CHAR(10), 'Contact Us|/contact-us', CHAR(10), 'Advertise With Us|/advertise', CHAR(10), 'Urdu|https://urdu.bolnews.com', CHAR(10), 'Live|/live'), CONCAT('Branded Content|/branded-content', CHAR(10), 'Privacy Policy|/privacy-policy', CHAR(10), 'Terms Of Service|/terms-of-service', CHAR(10), 'Editorial Policy|/editorial-policy', CHAR(10), 'Blog|/blogs'), TIMESTAMP '2026-10-21 00:00:00');

INSERT INTO `AdPlacements` (`Key`, `DisplayName`, `ZoneKey`, `ElementId`, `ProviderUnit`, `ContainerClass`, `IsEnabled`, `SortOrder`, `ParagraphIndex`, `InsertionMode`, `UpdatedAt`)
VALUES ('site-top-leaderboard', 'Top leaderboard', 'site.top', 'Bolnews_Top_Leaderboard_ATF', 'top-leaderboard-atf', 'bn-ad-slot-leaderboard', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('site-bottom-banner', 'Bottom banner', 'site.bottom', 'Bolnews_Bottom_BTF', 'bottom-btf', 'bn-ad-slot-banner', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('home-sidebar-top', 'Homepage sidebar top', 'home.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('home-sidebar-bottom', 'Homepage sidebar bottom', 'home.sidebar.bottom', 'Bolnews_Sidebar_Bottom_BTF', 'sidebar-bottom-btf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('home-content-incontent', 'Homepage content ad', 'home.content', 'Bolnews_Incontent_Lazy', 'incontent', 'bn-ad-slot-incontent', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('article-sidebar-top', 'Article sidebar top', 'article.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('article-sidebar-bottom', 'Article sidebar bottom', 'article.sidebar.bottom', 'Bolnews_Sidebar_Bottom_BTF', 'sidebar-bottom-btf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('article-inline-incontent', 'Article body ad', 'article.inline', 'Bolnews_Incontent_Lazy', 'incontent', 'bn-ad-slot-incontent', TRUE, 0, 2, 'afterParagraph', TIMESTAMP '2026-10-21 00:00:00'),
('category-sidebar-top', 'Category sidebar top', 'category.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('category-sidebar-bottom', 'Category sidebar bottom', 'category.sidebar.bottom', 'Bolnews_Sidebar_Bottom_BTF', 'sidebar-bottom-btf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('category-content-incontent', 'Category content ad', 'category.content', 'Bolnews_Incontent_Lazy', 'incontent', 'bn-ad-slot-incontent', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('tag-sidebar-top', 'Tag sidebar top', 'tag.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('tag-sidebar-bottom', 'Tag sidebar bottom', 'tag.sidebar.bottom', 'Bolnews_Sidebar_Bottom_BTF', 'sidebar-bottom-btf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('tag-content-incontent', 'Tag content ad', 'tag.content', 'Bolnews_Incontent_Lazy', 'incontent', 'bn-ad-slot-incontent', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('search-sidebar-top', 'Search sidebar top', 'search.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('search-sidebar-bottom', 'Search sidebar bottom', 'search.sidebar.bottom', 'Bolnews_Sidebar_Bottom_BTF', 'sidebar-bottom-btf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('search-content-incontent', 'Search results ad', 'search.content', 'Bolnews_Incontent_Lazy', 'incontent', 'bn-ad-slot-incontent', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00'),
('video-sidebar-top', 'Video sidebar top', 'video.sidebar.top', 'Bolnews_Sidebar_Top_ATF', 'sidebar-top-atf', 'bn-ad-slot-sidebar', TRUE, 0, NULL, 'zone', TIMESTAMP '2026-10-21 00:00:00');

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021145958_ProductizationSettingsAndAdPlacements', '8.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `AdPlacements` ADD `TargetPosition` varchar(16) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `AdPlacements` ADD `TargetSelector` varchar(500) CHARACTER SET utf8mb4 NULL;

UPDATE AdPlacements SET TargetPosition = 'beforeend' WHERE TargetPosition = ''

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021150312_ConfigurableAdTargetSelectors', '8.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD `CustomCss` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021150458_CustomPublisherStylesheet', '8.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD `LiveThumbnailUrl` varchar(512) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `SiteProfiles` ADD `LiveVideoEmbedUrl` varchar(1024) CHARACTER SET utf8mb4 NULL;

UPDATE SiteProfiles SET LiveVideoEmbedUrl = 'https://www.youtube.com/embed/9N6SWn1BQpA?rel=0&modestbranding=1', LiveThumbnailUrl = 'https://img.youtube.com/vi/9N6SWn1BQpA/maxresdefault.jpg' WHERE Id = 1

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261021151308_ConfigurableLivePlayer', '8.0.25');

COMMIT;

