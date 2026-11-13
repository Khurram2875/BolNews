-- Adds the public text-size settings to the site profile. Run after 20261111000000_AddSiteProfileLanguage.
START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD COLUMN `BaseFontSizePx` int NOT NULL DEFAULT 16;
ALTER TABLE `SiteProfiles` ADD COLUMN `ArticleFontSizePx` int NOT NULL DEFAULT 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261113000000_AddSiteProfileFontSizes', '8.0.25');

COMMIT;
