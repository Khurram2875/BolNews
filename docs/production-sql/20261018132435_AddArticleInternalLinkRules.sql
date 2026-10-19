-- EF Core migration: 20261018132435_AddArticleInternalLinkRules
-- Creates the ArticleInternalLinkRules table, its indexes and foreign keys,
-- then records this migration in __EFMigrationsHistory.
-- Prerequisite: all earlier migrations, including 20261002000000_AddMediaContentHash,
-- must already be applied and recorded in __EFMigrationsHistory.
-- Run once against the intended production database.

START TRANSACTION;

CREATE TABLE `ArticleInternalLinkRules` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `SourceArticleId` int NOT NULL,
    `TargetArticleId` int NULL,
    `RuleType` int NOT NULL,
    `AnchorText` varchar(500) CHARACTER SET utf8mb4 NULL,
    `TargetUrl` varchar(2048) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_ArticleInternalLinkRules` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_ArticleInternalLinkRules_Articles_SourceArticleId` FOREIGN KEY (`SourceArticleId`) REFERENCES `Articles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_ArticleInternalLinkRules_Articles_TargetArticleId` FOREIGN KEY (`TargetArticleId`) REFERENCES `Articles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ArticleInternalLinkRules_SourceArticleId` ON `ArticleInternalLinkRules` (`SourceArticleId`);

CREATE INDEX `IX_ArticleInternalLinkRules_TargetArticleId` ON `ArticleInternalLinkRules` (`TargetArticleId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261018132435_AddArticleInternalLinkRules', '8.0.25');

COMMIT;
