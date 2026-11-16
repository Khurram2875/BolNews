-- Publisher text-to-speech settings. Run after 20261115020000_AddPublisherHomeLayout.
START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD COLUMN `TtsSettingsJson` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261116000000_AddPublisherTts', '8.0.25');

COMMIT;
