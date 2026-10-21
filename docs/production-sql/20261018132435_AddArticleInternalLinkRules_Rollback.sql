-- Rollback for EF Core migration: 20261018132435_AddArticleInternalLinkRules
-- WARNING: This permanently deletes all internal-link rules stored in
-- ArticleInternalLinkRules. Back up the production database first.
-- Run only if this migration has been applied and no later migration depends
-- on this table. Restore the application version compatible with the rolled
-- back schema as part of the rollback.
-- MySQL DDL implicitly commits, so the table drop and history-row deletion
-- cannot be rolled back together as one transaction.

DROP TABLE IF EXISTS `ArticleInternalLinkRules`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20261018132435_AddArticleInternalLinkRules';
