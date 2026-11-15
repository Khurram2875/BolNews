START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `HomeLayoutJson`;

DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261115020000_AddPublisherHomeLayout';

COMMIT;
