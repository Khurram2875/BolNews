# BolNews Implementation Guide (from Solution Analysis)

This guide explains **how to implement** the suggested changes in small, safe PRs.

## Ground rules

- Keep one concern per PR.
- Run smoke checks before and after every PR.
- Merge only when all checks pass.

## PR-1: Startup pipeline cleanup (lowest risk)

### Goal
Remove duplicate authorization middleware without changing behavior.

### Files
- `BolNews.Web/Program.cs`

### Steps
1. Remove the second `app.UseAuthorization();` call (keep a single call).
2. Do not change route ordering.
3. Add a short code comment to lock intended middleware order.

### Verification
- Build succeeds.
- Home page loads.
- `/news/{categorySlug}/{slug}`, `/news/{categorySlug}`, `/author/{authorSlug}` all resolve.
- Admin login still works.

---

## PR-2: Cache invalidation hooks (safe freshness improvements)

### Goal
Keep existing caching but avoid stale home/related content after edits.

### Files (expected)
- `BolNews.Application/Common/CacheKeys.cs`
- `BolNews.Application/Services/CacheService.cs`
- `BolNews.Application/Services/ArticleService.cs`
- `BolNews.Web/Controllers/HomeController.cs` (only if key usage needs adjustment)

### Steps
1. Introduce central cache keys/tags (e.g., `homepage`, `related_{categoryId}_{articleId}`).
2. On article create/update/publish/delete, evict impacted keys.
3. Keep existing TTL values initially.
4. Prefer adding invalidation in service layer (single source of truth).

### Verification
- Edit an article and confirm homepage/related blocks refresh on next request.
- No cache-key mismatches between controller and service.

---

## PR-3: Mapping consistency (service-by-service)

### Goal
Reduce mapping drift without big-bang refactor.

### Files (example first service)
- `BolNews.Web/MappingProfile.cs`
- `BolNews.Application/Services/ArticleService.cs`
- `BolNews.Application/DTOs/ArticleDto.cs`
- Unit test project files (if present/added)

### Steps
1. Pick one service (start with `ArticleService`).
2. Add explicit AutoMapper profile mappings for that service only.
3. Replace manual map blocks gradually.
4. Add tests asserting critical fields map correctly.

### Verification
- Existing pages/actions using article DTOs return identical field values.
- No null regressions for optional fields.

---

## PR-4: Security hardening checks

### Goal
Close common admin/content risks.

### Files (expected)
- `BolNews.Web/Areas/Admin/Controllers/*.cs`
- Upload/image handling classes in `BolNews.Infrastructure` and/or `BolNews.Application`

### Steps
1. Ensure `[Authorize]` is present on all admin controllers/actions.
2. Ensure anti-forgery validation on all POST/PUT/DELETE form actions.
3. Enforce upload constraints:
   - extension whitelist
   - MIME/content validation
   - max file size
4. Return clear validation errors to admin UI.

### Verification
- Unauthorized access to admin endpoints is blocked.
- CSRF-protected forms reject invalid/missing tokens.
- Invalid file uploads are rejected.

---

## PR-5: Observability additions

### Goal
Make production issues diagnosable.

### Files (expected)
- `BolNews.Web/Controllers/*.cs`
- `BolNews.Application/Services/*.cs`
- Logging configuration in `appsettings*.json`

### Steps
1. Add structured logs at critical flows:
   - article publish/update
   - trend/weather external calls
2. Include correlation-friendly fields (article id, author id, slug, endpoint, latency).
3. Add warning/error logs for external dependency failures.

### Verification
- Logs show successful and failed external API calls with context.
- Publish/edit logs include identifiers for traceability.

---

## Smoke checklist (run for every PR)

1. Open home page.
2. Open one article details page.
3. Open one category listing page.
4. Open one author page.
5. Login to admin and open article list.
6. Create/edit one article in staging.

---

## Suggested command template

```bash
dotnet restore
dotnet build BolNews.sln
# If tests exist
dotnet test BolNews.sln
```

---

## Delivery order

1. PR-1 Startup cleanup
2. PR-2 Cache invalidation
3. PR-3 Mapping consistency
4. PR-4 Security checks
5. PR-5 Observability

This order minimizes blast radius and keeps behavior stable while improving reliability incrementally.
