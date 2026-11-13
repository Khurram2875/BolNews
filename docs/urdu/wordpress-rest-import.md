# Importing from WordPress over the REST API

Use this when you cannot (or do not want to) connect to the WordPress database, e.g. https://urdu.bolnews.com.

## Configure (appsettings / environment)
```json
"WordPressMigration": { "Source": "Rest" },
"WordPressRest": { "BaseUrl": "https://urdu.bolnews.com", "PerPage": 50 },
"WordPressMedia": { "LocalRoot": "", "AllowRemoteFallback": true }
```
`LocalRoot` stays empty because there is no local copy of `wp-content`; images are downloaded from the live site.
Private/draft content only: add `"Username"` and `"ApplicationPassword"` (WordPress → Users → Application Passwords) under `WordPressRest`; keep them in user-secrets or environment variables, never in the repository.

## Try it (nothing is written)
Sign in as admin and open
`/Admin/WordPressMigration/Test?from=2026-10-01&to=2026-11-12`
It returns the mapped articles (title, Urdu slug, category, tags, image URL, Yoast SEO title/description). Check a few, then use the normal Import form on `/Admin/WordPressMigration` with a small `take` (for example 50) first.

## What is mapped
| WordPress | CMS |
|---|---|
| post id | `SourceSystem=WordPress`, `SourceId` (re-running skips posts already imported) |
| title / excerpt / content | Title / Summary / Content (script, style and Elementor attributes removed) |
| slug (percent-encoded Urdu decoded) | Slug (made unique if needed) |
| primary category = first segment of the post URL (`/pakistan/861600/`) | Category (created if missing) |
| tags | Tags |
| author | Author (existing resolver) |
| featured image | downloaded and resized into article images; title kept as image title |
| Yoast title / description | MetaTitle / MetaDescription |

## Not covered yet
Old-URL redirects (`/category/<id>/` to the new article), the `post-featured-video` plugin field, inline images inside content, comments.

## "Latest News" (تازہ ترین) is never the article category
WordPress files every new post under Latest News as well as its real section. The importer ignores categories listed in `WordPressRest:ExcludedCategorySlugs` (default `latest-news`) and `ExcludedCategoryNames` (default `تازہ ترین`, `Latest News`). The category is the first segment of the post URL when usable, otherwise the first remaining WordPress category. A post that only has Latest News fails with a clear message instead of being filed wrongly.

Check already imported articles:
```sql
SELECT a.Id, a.Title, c.Slug FROM Articles a JOIN Categories c ON c.Id = a.CategoryId
WHERE a.SourceSystem = 'WordPress' AND (c.Slug = 'latest-news' OR c.Name = 'تازہ ترین');
```
