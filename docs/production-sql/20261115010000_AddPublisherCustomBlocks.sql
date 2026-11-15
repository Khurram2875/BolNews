-- Publisher custom HTML blocks. Run after 20261115000000_AddPublisherStyleControls.
START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD COLUMN `CustomBlocksJson` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261115010000_AddPublisherCustomBlocks', '8.0.25');

COMMIT;
