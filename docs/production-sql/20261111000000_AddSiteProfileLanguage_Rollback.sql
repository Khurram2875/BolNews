START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `LanguageCode`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261111000000_AddSiteProfileLanguage';

COMMIT;
