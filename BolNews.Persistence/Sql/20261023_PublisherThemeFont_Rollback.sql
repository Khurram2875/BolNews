-- Back up the database before running this rollback.
START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `FontFamily`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261023120000_AddSiteProfileFontFamily';

COMMIT;
