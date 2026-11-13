# Production SQL – productize-bolnews-cms line

Prerequisite: database already at migration `20261018132435_AddArticleInternalLinkRules`.

Run `Combined_Upgrade.sql` once (or the individual scripts in this order):

1. `BolNews.Persistence/Sql/20261021_Productization_Upgrade.sql` (covers 20261021145958, 20261021150312, 20261021150458, 20261021151308)
2. `BolNews.Persistence/Sql/20261023_PublisherThemeFont_Upgrade.sql`
3. `BolNews.Persistence/Sql/20261026_PublisherNavigationCategories_Upgrade.sql`
4. `docs/production-sql/20261029000000_AddLiveEvents.sql`
5. `docs/production-sql/20261030000000_AddLiveEventElectionCards.sql`
6. `docs/production-sql/20261103000000_AddLiveEventIntro.sql`
7. `docs/production-sql/20261104000000_AddSiteSettings.sql` (ticker speed key/value table)
8. `docs/production-sql/20261105000000_AddPolls.sql`
9. `docs/production-sql/20261111000000_AddSiteProfileLanguage.sql` (Urdu branch only: public site language column)

Rollback: `Combined_Rollback.sql` (reverse order). Skip any step already applied (check `__EFMigrationsHistory`).
Local dev: `Update-Database` applies the same chain.
10. `docs/production-sql/20261113000000_AddSiteProfileFontSizes.sql` (Urdu branch: base/article text size)
