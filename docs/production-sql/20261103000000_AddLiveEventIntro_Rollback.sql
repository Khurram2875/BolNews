-- Rollback for EF Core migration: 20261103000000_AddLiveEventIntro
-- WARNING: permanently deletes the intro text of every Live Event. Back up first.
-- Restore an application version that does not expect this column before running.

ALTER TABLE `LiveEvents` DROP COLUMN `IntroHtml`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261103000000_AddLiveEventIntro';
