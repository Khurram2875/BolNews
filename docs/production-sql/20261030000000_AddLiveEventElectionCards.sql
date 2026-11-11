-- EF Core migration: 20261030000000_AddLiveEventElectionCards
-- Adds the US election scoreboard card to Live Events: an event type and three
-- card label columns on LiveEvents, plus the LiveEventCandidates table.
-- Prerequisite: 20261029000000_AddLiveEvents has already been applied
-- (run 20261029000000_AddLiveEvents.sql first, in that order).
-- Back up the database first. Run once against the intended production database.
-- Do not run if these columns/tables already exist from a failed/partial attempt.

ALTER TABLE `LiveEvents` ADD `EventType` int NOT NULL DEFAULT 0;
ALTER TABLE `LiveEvents` ADD `ElectionHeader` varchar(100) CHARACTER SET utf8mb4 NULL;
ALTER TABLE `LiveEvents` ADD `RaceTitle` varchar(100) CHARACTER SET utf8mb4 NULL;
ALTER TABLE `LiveEvents` ADD `RaceSubtitle` varchar(100) CHARACTER SET utf8mb4 NULL;

CREATE TABLE `LiveEventCandidates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LiveEventId` int NOT NULL,
    `Party` int NOT NULL,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Percentage` decimal(5,2) NOT NULL,
    `Votes` int NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_LiveEventCandidates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEventCandidates_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_LiveEventCandidates_LiveEventId_SortOrder`
    ON `LiveEventCandidates` (`LiveEventId`, `SortOrder`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261030000000_AddLiveEventElectionCards', '8.0.25');
