-- Productize line: Productization + Live Events + Site Settings(ticker) + Polls UPGRADE
-- MySQL / MariaDB. Back up the database first. Run top to bottom, once.
-- Each section is guarded by __EFMigrationsHistory where its source script is; do not reorder.


-- ======== BEGIN BolNews.Persistence/Sql/20261021_Productization_Upgrade.sql ========
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
-- ======== END BolNews.Persistence/Sql/20261021_Productization_Upgrade.sql ========

-- ======== BEGIN BolNews.Persistence/Sql/20261023_PublisherThemeFont_Upgrade.sql ========
-- Run this on databases that already contain the productization SiteProfiles table.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `FontFamily` varchar(80) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Inter';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261023120000_AddSiteProfileFontFamily', '8.0.25');

COMMIT;
-- ======== END BolNews.Persistence/Sql/20261023_PublisherThemeFont_Upgrade.sql ========

-- ======== BEGIN BolNews.Persistence/Sql/20261026_PublisherNavigationCategories_Upgrade.sql ========
-- Adds the publisher-configurable category order to existing SiteProfiles tables.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `NavigationCategoryIds` varchar(100) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261026120000_AddPublisherNavigationCategories', '8.0.25');

COMMIT;
-- ======== END BolNews.Persistence/Sql/20261026_PublisherNavigationCategories_Upgrade.sql ========

-- ======== BEGIN docs/production-sql/20261029000000_AddLiveEvents.sql ========
-- EF Core migration: 20261029000000_AddLiveEvents
-- Adds Live Events, their updates, and links to existing articles/media.
-- Slug is varchar(191) so its utf8mb4 unique index fits older MySQL/MariaDB
-- servers with the 767-byte InnoDB index-key limit.
-- Prerequisites: the Articles, MediaAssets, and __EFMigrationsHistory tables
-- exist, and all earlier migrations are applied. Back up the database first.
-- Run once against the intended production database. Do not run if this
-- migration's tables already exist from a failed/partial attempt; see notes
-- in the deployment instructions before proceeding.

CREATE TABLE `LiveEvents` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(191) CHARACTER SET utf8mb4 NOT NULL,
    `FeaturedMediaId` int NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `IsPinned` tinyint(1) NOT NULL,
    `ShowLiveBadge` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_LiveEvents` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEvents_MediaAssets_FeaturedMediaId`
        FOREIGN KEY (`FeaturedMediaId`) REFERENCES `MediaAssets` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `LiveEventUpdates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LiveEventId` int NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `Summary` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `Details` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Source` varchar(500) CHARACTER SET utf8mb4 NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_LiveEventUpdates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEventUpdates_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `LiveEventArticles` (
    `LiveEventId` int NOT NULL,
    `ArticleId` int NOT NULL,
    CONSTRAINT `PK_LiveEventArticles` PRIMARY KEY (`LiveEventId`, `ArticleId`),
    CONSTRAINT `FK_LiveEventArticles_Articles_ArticleId`
        FOREIGN KEY (`ArticleId`) REFERENCES `Articles` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LiveEventArticles_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_LiveEvents_FeaturedMediaId`
    ON `LiveEvents` (`FeaturedMediaId`);
CREATE INDEX `IX_LiveEvents_IsActive_IsPinned_CreatedAt`
    ON `LiveEvents` (`IsActive`, `IsPinned`, `CreatedAt`);
CREATE UNIQUE INDEX `IX_LiveEvents_Slug`
    ON `LiveEvents` (`Slug`);
CREATE INDEX `IX_LiveEventUpdates_LiveEventId_Timestamp`
    ON `LiveEventUpdates` (`LiveEventId`, `Timestamp`);
CREATE INDEX `IX_LiveEventArticles_ArticleId`
    ON `LiveEventArticles` (`ArticleId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261029000000_AddLiveEvents', '8.0.25');
-- ======== END docs/production-sql/20261029000000_AddLiveEvents.sql ========

-- ======== BEGIN docs/production-sql/20261030000000_AddLiveEventElectionCards.sql ========
-- EF Core migration: 20261030000000_AddLiveEventElectionCards
-- Adds the US election scoreboard card to Live Events: an event type and three
-- card label columns on LiveEvents, plus the LiveEventCandidates table.
-- Prerequisite: 20261029000000_AddLiveEvents has already been applied
-- (run 20261029000000_AddLiveEvents.sql first, in that order).
-- Back up the database first. Run once against the intended production database.
-- Do not run if these columns/tables already exist from a failed/partial attempt.

ALTER TABLE `LiveEvents` ADD `EventType` int NOT NULL DEFAULT 0;
ALTER TABLE `LiveEvents` ADD `ElectionHeader` varchar(100) CHARACTER SET utf8mb4 NULL;
ALTER TABLE `LiveEvents` ADD `RaceTitle` varchar(100) CHARACTER SET utf8mb4 NULL;
ALTER TABLE `LiveEvents` ADD `RaceSubtitle` varchar(100) CHARACTER SET utf8mb4 NULL;

CREATE TABLE `LiveEventCandidates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LiveEventId` int NOT NULL,
    `Party` int NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Percentage` decimal(5,2) NOT NULL,
    `Votes` int NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_LiveEventCandidates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEventCandidates_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_LiveEventCandidates_LiveEventId_SortOrder`
    ON `LiveEventCandidates` (`LiveEventId`, `SortOrder`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261030000000_AddLiveEventElectionCards', '8.0.25');
-- ======== END docs/production-sql/20261030000000_AddLiveEventElectionCards.sql ========

-- ======== BEGIN docs/production-sql/20261103000000_AddLiveEventIntro.sql ========
-- EF Core migration: 20261103000000_AddLiveEventIntro
-- Adds the rich-text intro box (shown beside the image on the Live Event page).
-- Prerequisites, in order: 20261029000000_AddLiveEvents, 20261030000000_AddLiveEventElectionCards.
-- Back up the database first. Run once against the intended production database.

ALTER TABLE `LiveEvents` ADD `IntroHtml` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261103000000_AddLiveEventIntro', '8.0.25');
-- ======== END docs/production-sql/20261103000000_AddLiveEventIntro.sql ========

-- ======== BEGIN docs/production-sql/20261104000000_AddSiteSettings.sql ========
-- EF Core migration: 20261104000000_AddSiteSettings
-- Adds the SiteSettings key/value table (used for the headline ticker speed).
-- Back up the database first. Run once against the intended production database.
-- No rows are needed: the site uses its default speed until an admin saves one.

CREATE TABLE `SiteSettings` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Key` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Value` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_SiteSettings` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_SiteSettings_Key` ON `SiteSettings` (`Key`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261104000000_AddSiteSettings', '8.0.25');
-- ======== END docs/production-sql/20261104000000_AddSiteSettings.sql ========

-- ======== BEGIN docs/production-sql/20261105000000_AddPolls.sql ========
-- EF Core migration: 20261105000000_AddPolls
-- Adds the Polls feature: Polls, PollOptions, PollVotes.
-- Prerequisite: the MediaAssets table exists (it does in every environment).
-- Back up the database first. Run once against the intended production database.

CREATE TABLE `Polls` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `PollType` int NOT NULL,
    `FeaturedMediaId` int NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `EndsAt` datetime(6) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Polls` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Polls_MediaAssets_FeaturedMediaId`
        FOREIGN KEY (`FeaturedMediaId`) REFERENCES `MediaAssets` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PollOptions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PollId` int NOT NULL,
    `Text` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_PollOptions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PollOptions_Polls_PollId`
        FOREIGN KEY (`PollId`) REFERENCES `Polls` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PollVotes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PollId` int NOT NULL,
    `OptionId` int NOT NULL,
    `VoterHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `IpHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_PollVotes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PollVotes_Polls_PollId`
        FOREIGN KEY (`PollId`) REFERENCES `Polls` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_PollVotes_PollOptions_OptionId`
        FOREIGN KEY (`OptionId`) REFERENCES `PollOptions` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Polls_FeaturedMediaId` ON `Polls` (`FeaturedMediaId`);
CREATE INDEX `IX_Polls_IsActive_EndsAt` ON `Polls` (`IsActive`, `EndsAt`);
CREATE INDEX `IX_PollOptions_PollId_SortOrder` ON `PollOptions` (`PollId`, `SortOrder`);
CREATE UNIQUE INDEX `IX_PollVotes_PollId_VoterHash` ON `PollVotes` (`PollId`, `VoterHash`);
CREATE INDEX `IX_PollVotes_PollId_OptionId` ON `PollVotes` (`PollId`, `OptionId`);
CREATE INDEX `IX_PollVotes_PollId_IpHash` ON `PollVotes` (`PollId`, `IpHash`);
CREATE INDEX `IX_PollVotes_OptionId` ON `PollVotes` (`OptionId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261105000000_AddPolls', '8.0.25');
-- ======== END docs/production-sql/20261105000000_AddPolls.sql ========

-- ======== BEGIN docs/production-sql/20261111000000_AddSiteProfileLanguage.sql ========
-- Adds the public site language (en / ur) to the site profile.
-- Run after 20261105000000_AddPolls on the productize/Urdu line.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `LanguageCode` varchar(8) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'en';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261111000000_AddSiteProfileLanguage', '8.0.25');

COMMIT;
-- ======== END docs/production-sql/20261111000000_AddSiteProfileLanguage.sql ========
