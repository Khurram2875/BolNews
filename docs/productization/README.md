# BolNews CMS productization

## Customer model

The productized CMS currently supports one publisher per application deployment and database. Each customer can have an independent deployment and `SiteProfile`; the current design is not a shared multi-tenant service where many customers share one database and application process.

Use this branch as the shared product core. Prefer configuring a customer in the admin area. Create a customer-specific branch only when that publisher needs behavior or page structures that the current configuration points cannot express. Keep those customer-only changes small so they can be rebased on future product releases.

## Runtime configuration

Administrators can open **Administration → Publisher Settings** to update publisher names, upload logos and a favicon, configure livestream and language-edition links, brand colors, base theme, site-wide font family, metadata, social links, copyright, footer links, page copy, additional CSS, and the public navigation category order. Home and Latest remain fixed; up to nine parent categories can be selected and ordered. The selected color theme and font apply to both public pages and the admin workspace. Custom CSS is served separately at `/site-theme.css`, so it can be updated without rebuilding the application.

The color theme is selected by the publisher and is not overridden by a visitor's old browser local-storage setting. Theme presets establish the page/surface/text palette, while Primary and Accent colors drive interactive and branded components. Font choices are allow-listed; a selected hosted web font is requested from Google Fonts, while System UI, Georgia, and Arial use local system fonts. The `SiteProfiles.FontFamily` field is added by migration `20261023120000_AddSiteProfileFontFamily`; SQL upgrade and rollback files are available in `BolNews.Persistence/Sql`.

Policy and information page text is stored as plain text and HTML-encoded by Razor. Empty pages show a setup notice rather than BOL-specific legal copy. Each publisher should enter its own reviewed policy text before launch. The `SiteProfiles.NavigationCategoryIds` field is added by migration `20261026120000_AddPublisherNavigationCategories`; matching SQL upgrade and rollback scripts are available in `BolNews.Persistence/Sql`.

Under **Administration → Ad Placements**, administrators can configure provider script URL (in Publisher Settings), ad div ID, provider unit, enabled state, order, container classes, standard page zone, and optional CSS target selector/insertion position. Article body placements also support a paragraph position. A target selector is applied only on pages where that selected zone is rendered; if it does not match, the ad remains at its standard zone (or the configured article paragraph fallback).

## Database and deployment

For a new customer installation, follow [Fresh installation](installation.md) to create a dedicated database and runtime connection string, apply the current migrations, and create the first CMS Admin account.

The product settings and ad placements require EF Core migrations. SQL files in `BolNews.Persistence/Sql` are generated from the migrations and include the `__EFMigrationsHistory` inserts. Confirm the production database is at the documented starting migration before applying the upgrade script. The rollback script drops the newly introduced configuration tables and columns; back up the database first because customer-edited settings and ad placements will be lost when those tables are dropped.

The first migration seeds the existing BOL profile and ad positions so the current publisher retains its visible identity and advertising setup after upgrade. New customer installations should edit these values during deployment setup.

## Current product boundary

Brand identity, theme colors, a custom stylesheet, legal page text, footer links, logos, ad script, ad div IDs, provider units, and ad placement positions are configuration-driven. The available page templates and the zones from which an ad is rendered are still part of the shared application. Adding a new page type, a brand-new CMS module, or a new content structure may still require a core code change.
