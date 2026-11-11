-- Rollback for EF Core migration: 20261104000000_AddSiteSettings
-- WARNING: deletes all saved site settings (the ticker reverts to its default speed). Back up first.

DROP TABLE IF EXISTS `SiteSettings`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261104000000_AddSiteSettings';
