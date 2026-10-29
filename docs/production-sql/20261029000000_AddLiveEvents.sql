-- EF Core migration: 20261029000000_AddLiveEvents
-- Adds Live Events, their updates, and links to existing articles/media.
-- Slug is varchar(191) so its utf8mb4 unique index fits older MySQL/MariaDB
-- servers with the 767-byte InnoDB index-key limit.
-- Prerequisites: the Articles, MediaAssets, and __EFMigrationsHistory tables
-- exist, and all earlier migrations are applied. Back up the database first.
-- Run once against the intended production database. Do not run if this
-- migration's tables already exist from a failed/partial attempt; see notes
-- in the deployment instructions before proceeding.

CREATE TABLE `LiveEvents` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(250) CHARACTER SET utf8mb4 NOT NULL,
    `Slug` varchar(191) CHARACTER SET utf8mb4 NOT NULL,
    `FeaturedMediaId` int NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `IsPinned` tinyint(1) NOT NULL,
    `ShowLiveBadge` tinyint(1) NOT NULL,
    `CreatedAt` datetime(6) NOT NULL,
    `CreatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `UpdatedAt` datetime(6) NULL,
    `UpdatedBy` longtext CHARACTER SET utf8mb4 NULL,
    `IsDeleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_LiveEvents` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEvents_MediaAssets_FeaturedMediaId`
        FOREIGN KEY (`FeaturedMediaId`) REFERENCES `MediaAssets` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `LiveEventUpdates` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `LiveEventId` int NOT NULL,
    `Timestamp` datetime(6) NOT NULL,
    `Summary` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `Details` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Source` varchar(500) CHARACTER SET utf8mb4 NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_LiveEventUpdates` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_LiveEventUpdates_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `LiveEventArticles` (
    `LiveEventId` int NOT NULL,
    `ArticleId` int NOT NULL,
    CONSTRAINT `PK_LiveEventArticles` PRIMARY KEY (`LiveEventId`, `ArticleId`),
    CONSTRAINT `FK_LiveEventArticles_Articles_ArticleId`
        FOREIGN KEY (`ArticleId`) REFERENCES `Articles` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LiveEventArticles_LiveEvents_LiveEventId`
        FOREIGN KEY (`LiveEventId`) REFERENCES `LiveEvents` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_LiveEvents_FeaturedMediaId`
    ON `LiveEvents` (`FeaturedMediaId`);
CREATE INDEX `IX_LiveEvents_IsActive_IsPinned_CreatedAt`
    ON `LiveEvents` (`IsActive`, `IsPinned`, `CreatedAt`);
CREATE UNIQUE INDEX `IX_LiveEvents_Slug`
    ON `LiveEvents` (`Slug`);
CREATE INDEX `IX_LiveEventUpdates_LiveEventId_Timestamp`
    ON `LiveEventUpdates` (`LiveEventId`, `Timestamp`);
CREATE INDEX `IX_LiveEventArticles_ArticleId`
    ON `LiveEventArticles` (`ArticleId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261029000000_AddLiveEvents', '8.0.25');
