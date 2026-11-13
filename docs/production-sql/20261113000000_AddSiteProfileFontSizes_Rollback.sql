START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `ArticleFontSizePx`;
ALTER TABLE `SiteProfiles` DROP COLUMN `BaseFontSizePx`;

DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261113000000_AddSiteProfileFontSizes';

COMMIT;
