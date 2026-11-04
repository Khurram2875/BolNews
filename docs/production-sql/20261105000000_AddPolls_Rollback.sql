-- Rollback for EF Core migration: 20261105000000_AddPolls
-- WARNING: permanently deletes every poll, its options and all votes. Back up first.
-- Restore an application version without the Polls feature before running.

DROP TABLE IF EXISTS `PollVotes`;
DROP TABLE IF EXISTS `PollOptions`;
DROP TABLE IF EXISTS `Polls`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261105000000_AddPolls';
