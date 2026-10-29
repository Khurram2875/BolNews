-- Rollback for EF Core migration: 20261029000000_AddLiveEvents
-- WARNING: this permanently deletes all Live Events, updates, and article links.
-- Back up the database first. Run only after restoring an application version
-- that does not expect these tables, and only if no later migration depends on
-- them. MySQL/MariaDB DDL implicitly commits, so this is not an atomic rollback.

DROP TABLE IF EXISTS `LiveEventArticles`;
DROP TABLE IF EXISTS `LiveEventUpdates`;
DROP TABLE IF EXISTS `LiveEvents`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261029000000_AddLiveEvents';
