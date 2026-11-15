START TRANSACTION;

DROP TABLE `SiteProfileVersions`;
ALTER TABLE `SiteProfiles` DROP COLUMN `StyleSettingsJson`;

DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261115000000_AddPublisherStyleControls';

COMMIT;
