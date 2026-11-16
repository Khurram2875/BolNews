START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `TtsSettingsJson`;

DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261116000000_AddPublisherTts';

COMMIT;
