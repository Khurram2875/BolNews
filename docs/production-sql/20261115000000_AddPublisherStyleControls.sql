-- Publisher style controls + version history. Run after 20261113000000_AddSiteProfileFontSizes.
START TRANSACTION;

ALTER TABLE `SiteProfiles` ADD COLUMN `StyleSettingsJson` longtext CHARACTER SET utf8mb4 NULL;

CREATE TABLE `SiteProfileVersions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Note` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `SnapshotJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_SiteProfileVersions` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_SiteProfileVersions_CreatedAt` ON `SiteProfileVersions` (`CreatedAt`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261115000000_AddPublisherStyleControls', '8.0.25');

COMMIT;
