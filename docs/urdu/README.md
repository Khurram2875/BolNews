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
- **Nafees Web Naskh** (CRULP): drop `NafeesWebNaskh.woff2` or `NafeesWebNaskh.ttf` into `wwwroot/fonts/`. Check the licence text shipped with your copy before committing it.
- A hosted font only shows in the dropdown when its file exists in `wwwroot/fonts`.
- Alvi Nastaleeq's embedded licence allows educational/research use only and forbids redistribution or modification without written permission. The file is therefore git-ignored: copy it to each server manually, and only if you hold that permission.
- To add another hosted font: put the file in `wwwroot/fonts`, add it to `FontStacks`, `UrduFonts` and `LocalFonts` in `Services/PublisherThemeCatalog.cs`.
- **Nafees Naskh** (CRULP, `NafeesNaskh.ttf` + `NafeesNaskh-LICENSE.txt`, committed): the CRULP licence allows free use and redistribution provided the licence text stays with the font and the font is not renamed or modified.
- **Al Qalam Fakhar Naveed, Haider, Shamsheer Unicode**: downloaded from urdufonts.pk with "All rights reserved" and no redistribution licence, so they are git-ignored. Copy `AlQalamFakharNaveed.ttf`, `Haider.ttf` and `ShamsheerUnicode.ttf` into `wwwroot/fonts/` on each server if you are satisfied with their terms.
