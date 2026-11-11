-- EF Core migration: 20261105000000_AddPolls
-- Adds the Polls feature: Polls, PollOptions, PollVotes.
-- Prerequisite: the MediaAssets table exists (it does in every environment).
-- Back up the database first. Run once against the intended production database.

CREATE TABLE `Polls` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `PollType` int NOT NULL,
    `FeaturedMediaId` int NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `EndsAt` datetime(6) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Polls` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Polls_MediaAssets_FeaturedMediaId`
        FOREIGN KEY (`FeaturedMediaId`) REFERENCES `MediaAssets` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PollOptions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PollId` int NOT NULL,
    `Text` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `SortOrder` int NOT NULL,
    CONSTRAINT `PK_PollOptions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PollOptions_Polls_PollId`
        FOREIGN KEY (`PollId`) REFERENCES `Polls` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PollVotes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PollId` int NOT NULL,
    `OptionId` int NOT NULL,
    `VoterHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `IpHash` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_PollVotes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PollVotes_Polls_PollId`
        FOREIGN KEY (`PollId`) REFERENCES `Polls` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_PollVotes_PollOptions_OptionId`
        FOREIGN KEY (`OptionId`) REFERENCES `PollOptions` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Polls_FeaturedMediaId` ON `Polls` (`FeaturedMediaId`);
CREATE INDEX `IX_Polls_IsActive_EndsAt` ON `Polls` (`IsActive`, `EndsAt`);
CREATE INDEX `IX_PollOptions_PollId_SortOrder` ON `PollOptions` (`PollId`, `SortOrder`);
CREATE UNIQUE INDEX `IX_PollVotes_PollId_VoterHash` ON `PollVotes` (`PollId`, `VoterHash`);
CREATE INDEX `IX_PollVotes_PollId_OptionId` ON `PollVotes` (`PollId`, `OptionId`);
CREATE INDEX `IX_PollVotes_PollId_IpHash` ON `PollVotes` (`PollId`, `IpHash`);
CREATE INDEX `IX_PollVotes_OptionId` ON `PollVotes` (`OptionId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261105000000_AddPolls', '8.0.25');
