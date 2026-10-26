-- Adds the publisher-configurable category order to existing SiteProfiles tables.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    ADD COLUMN `NavigationCategoryIds` varchar(100) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261026120000_AddPublisherNavigationCategories', '8.0.25');

COMMIT;
