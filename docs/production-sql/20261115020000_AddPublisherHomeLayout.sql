-- Publisher home page layout settings. Run after 20261115010000_AddPublisherCustomBlocks.
START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD COLUMN `HomeLayoutJson` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261115020000_AddPublisherHomeLayout', '8.0.25');

COMMIT;
