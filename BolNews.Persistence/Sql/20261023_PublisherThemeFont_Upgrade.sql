-- Run this on databases that already contain the productization SiteProfiles table.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `FontFamily` varchar(80) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Inter';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261023120000_AddSiteProfileFontFamily', '8.0.25');

COMMIT;
