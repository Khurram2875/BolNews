-- Rollback for EF Core migration: 20261030000000_AddLiveEventElectionCards
-- WARNING: this permanently deletes all election candidates and the election
-- card labels/type on every Live Event. Back up the database first.
-- Run this BEFORE rolling back 20261029000000_AddLiveEvents (if that is also
-- being rolled back), and only after restoring an application version that does
-- not expect these columns/tables. MySQL/MariaDB DDL implicitly commits, so this
-- is not an atomic rollback.

DROP TABLE IF EXISTS `LiveEventCandidates`;

ALTER TABLE `LiveEvents` DROP COLUMN `RaceSubtitle`;
ALTER TABLE `LiveEvents` DROP COLUMN `RaceTitle`;
ALTER TABLE `LiveEvents` DROP COLUMN `ElectionHeader`;
ALTER TABLE `LiveEvents` DROP COLUMN `EventType`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261030000000_AddLiveEventElectionCards';
