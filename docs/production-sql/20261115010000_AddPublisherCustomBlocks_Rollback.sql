START TRANSACTION;

ALTER TABLE `SiteProfiles` DROP COLUMN `CustomBlocksJson`;

DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261115010000_AddPublisherCustomBlocks';

COMMIT;
