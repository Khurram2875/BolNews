-- EF Core migration: 20261104000000_AddSiteSettings
-- Adds the SiteSettings key/value table (used for the headline ticker speed).
-- Back up the database first. Run once against the intended production database.
-- No rows are needed: the site uses its default speed until an admin saves one.

CREATE TABLE `SiteSettings` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Key` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Value` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_SiteSettings` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_SiteSettings_Key` ON `SiteSettings` (`Key`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261104000000_AddSiteSettings', '8.0.25');
