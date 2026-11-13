# Urdu (right-to-left) deployment

The Urdu CMS is the same codebase deployed as a separate site with its own database. Nothing is forked: the language is a
site setting.

## Turn it on
- New install: the installer asks for "Public site language" – answer `ur`.
- Existing site: run `docs/production-sql/20261111000000_AddSiteProfileLanguage.sql`, then Admin → Site Settings → *Public site language* → Urdu.

## What changes when the language is `ur`
- Public pages: `<html lang="ur" dir="rtl">`, Bootstrap RTL, mirrored site CSS, Noto Nastaliq Urdu font, Urdu labels, Urdu dates and "x منٹ پہلے" times, ticker scrolling right-to-left.
- Admin area stays English / left-to-right. Text fields use `dir="auto"` and the rich-text editors (articles, live events, poll text) type right-to-left in Nastaliq.
- Navigation comes from categories: create the categories with Urdu names (slugs can stay English).

## Maintenance
- After editing `wwwroot/css/site.css`, `pages/home.css` or `pages/live.css`, regenerate the mirrored copies: `python3 tools/build-rtl-css.py` and commit the `*.rtl.css` files.
- Fixed labels live in `BolNews.Web/Services/UiText.cs` (Razor: `@T["Latest"]`). A missing Urdu entry falls back to English.
- Typography-only tweaks go in `wwwroot/css/rtl.css`.

## Known gaps
- Account/login pages, error text from the server other than polls, and a few admin-only messages are still English.
- Inline `<style>` blocks inside some views are not mirrored automatically.
- Not yet run against a real browser session with the full app (no .NET SDK in the build sandbox); check each page type once on a test server.

## Fonts (Publisher Settings → Site font family)
- Google fonts (loaded on demand): Noto Nastaliq Urdu, Noto Naskh Arabic, Gulzar, Lateef, Scheherazade New, Amiri.
- Hosted on this site (`BolNews.Web/wwwroot/fonts/`): **Jameel Noori Nastaleeq** (`JameelNooriNastaleeq.woff2`, committed, about 6.9 MB, subset to Urdu/Latin) and **Alvi Nastaleeq** (`AlviNastaleeq.ttf`).
- A hosted font only shows in the dropdown when its file exists in `wwwroot/fonts`.
- Alvi Nastaleeq's embedded licence allows educational/research use only and forbids redistribution or modification without written permission. The file is therefore git-ignored: copy it to each server manually, and only if you hold that permission.
- To add another hosted font: put the file in `wwwroot/fonts`, add it to `FontStacks`, `UrduFonts` and `LocalFonts` in `Services/PublisherThemeCatalog.cs`.
