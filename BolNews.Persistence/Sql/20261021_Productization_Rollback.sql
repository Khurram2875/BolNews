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

