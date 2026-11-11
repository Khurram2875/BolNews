-- Productize line: ROLLBACK (reverse order) of Combined_Upgrade.sql
-- MySQL / MariaDB. Back up the database first. Run top to bottom, once.
-- Each section is guarded by __EFMigrationsHistory where its source script is; do not reorder.


-- ======== BEGIN docs/production-sql/20261105000000_AddPolls_Rollback.sql ========
-- Rollback for EF Core migration: 20261105000000_AddPolls
-- WARNING: permanently deletes every poll, its options and all votes. Back up first.
-- Restore an application version without the Polls feature before running.

DROP TABLE IF EXISTS `PollVotes`;
DROP TABLE IF EXISTS `PollOptions`;
DROP TABLE IF EXISTS `Polls`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261105000000_AddPolls';
-- ======== END docs/production-sql/20261105000000_AddPolls_Rollback.sql ========

-- ======== BEGIN docs/production-sql/20261104000000_AddSiteSettings_Rollback.sql ========
-- Rollback for EF Core migration: 20261104000000_AddSiteSettings
-- WARNING: deletes all saved site settings (the ticker reverts to its default speed). Back up first.

DROP TABLE IF EXISTS `SiteSettings`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261104000000_AddSiteSettings';
-- ======== END docs/production-sql/20261104000000_AddSiteSettings_Rollback.sql ========

-- ======== BEGIN docs/production-sql/20261103000000_AddLiveEventIntro_Rollback.sql ========
-- Rollback for EF Core migration: 20261103000000_AddLiveEventIntro
-- WARNING: permanently deletes the intro text of every Live Event. Back up first.
-- Restore an application version that does not expect this column before running.

ALTER TABLE `LiveEvents` DROP COLUMN `IntroHtml`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261103000000_AddLiveEventIntro';
-- ======== END docs/production-sql/20261103000000_AddLiveEventIntro_Rollback.sql ========

-- ======== BEGIN docs/production-sql/20261030000000_AddLiveEventElectionCards_Rollback.sql ========
-- Rollback for EF Core migration: 20261030000000_AddLiveEventElectionCards
-- WARNING: this permanently deletes all election candidates and the election
-- card labels/type on every Live Event. Back up the database first.
-- Run this BEFORE rolling back 20261029000000_AddLiveEvents (if that is also
-- being rolled back), and only after restoring an application version that does
-- not expect these columns/tables. MySQL/MariaDB DDL implicitly commits, so this
-- is not an atomic rollback.

DROP TABLE IF EXISTS `LiveEventCandidates`;

ALTER TABLE `LiveEvents` DROP COLUMN `RaceSubtitle`;
ALTER TABLE `LiveEvents` DROP COLUMN `RaceTitle`;
ALTER TABLE `LiveEvents` DROP COLUMN `ElectionHeader`;
ALTER TABLE `LiveEvents` DROP COLUMN `EventType`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261030000000_AddLiveEventElectionCards';
-- ======== END docs/production-sql/20261030000000_AddLiveEventElectionCards_Rollback.sql ========

-- ======== BEGIN docs/production-sql/20261029000000_AddLiveEvents_Rollback.sql ========
-- Rollback for EF Core migration: 20261029000000_AddLiveEvents
-- WARNING: this permanently deletes all Live Events, updates, and article links.
-- Back up the database first. Run only after restoring an application version
-- that does not expect these tables, and only if no later migration depends on
-- them. MySQL/MariaDB DDL implicitly commits, so this is not an atomic rollback.

DROP TABLE IF EXISTS `LiveEventArticles`;
DROP TABLE IF EXISTS `LiveEventUpdates`;
DROP TABLE IF EXISTS `LiveEvents`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261029000000_AddLiveEvents';
-- ======== END docs/production-sql/20261029000000_AddLiveEvents_Rollback.sql ========

-- ======== BEGIN BolNews.Persistence/Sql/20261026_PublisherNavigationCategories_Rollback.sql ========
-- Run only if rolling back 20261026120000_AddPublisherNavigationCategories.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    DROP COLUMN `NavigationCategoryIds`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261026120000_AddPublisherNavigationCategories';

COMMIT;
-- ======== END BolNews.Persistence/Sql/20261026_PublisherNavigationCategories_Rollback.sql ========

-- ======== BEGIN BolNews.Persistence/Sql/20261023_PublisherThemeFont_Rollback.sql ========
-- Back up the database before running this rollback.
START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `FontFamily`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261023120000_AddSiteProfileFontFamily';

COMMIT;
-- ======== END BolNews.Persistence/Sql/20261023_PublisherThemeFont_Rollback.sql ========

-- ======== BEGIN BolNews.Persistence/Sql/20261021_Productization_Rollback.sql ========
START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `LiveThumbnailUrl`;

ALTER TABLE `SiteProfiles` DROP COLUMN `LiveVideoEmbedUrl`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261021151308_ConfigurableLivePlayer';

COMMIT;

START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `CustomCss`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261021150458_CustomPublisherStylesheet';

COMMIT;

START TRANSACTION;

ALTER TABLE `AdPlacements` DROP COLUMN `TargetPosition`;

ALTER TABLE `AdPlacements` DROP COLUMN `TargetSelector`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261021150312_ConfigurableAdTargetSelectors';

COMMIT;

START TRANSACTION;

DROP TABLE `AdPlacements`;

DROP TABLE `SiteProfiles`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261021145958_ProductizationSettingsAndAdPlacements';

COMMIT;
-- ======== END BolNews.Persistence/Sql/20261021_Productization_Rollback.sql ========
