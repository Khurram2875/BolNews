-- Run only if rolling back 20261026120000_AddPublisherNavigationCategories.
START TRANSACTION;

ALTER TABLE `SiteProfiles`
    DROP COLUMN `NavigationCategoryIds`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261026120000_AddPublisherNavigationCategories';

COMMIT;
