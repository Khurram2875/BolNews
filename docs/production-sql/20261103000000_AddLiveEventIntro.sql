-- EF Core migration: 20261103000000_AddLiveEventIntro
-- Adds the rich-text intro box (shown beside the image on the Live Event page).
-- Prerequisites, in order: 20261029000000_AddLiveEvents, 20261030000000_AddLiveEventElectionCards.
-- Back up the database first. Run once against the intended production database.

ALTER TABLE `LiveEvents` ADD `IntroHtml` longtext CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261103000000_AddLiveEventIntro', '8.0.25');
