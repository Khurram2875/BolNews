-- Adds the public site language (en / ur) to the site profile.
-- Run after 20261105000000_AddPolls on the productize/Urdu line.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `LanguageCode` varchar(8) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'en';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261111000000_AddSiteProfileLanguage', '8.0.25');

COMMIT;
