# Bol News Performance Optimization — September 2026

## Baseline

PageSpeed Insights mobile report supplied on 2026-09-16:

- Performance: 47
- Accessibility: 90
- Best Practices: 54
- SEO: 92
- First Contentful Paint: 4.2 s
- Largest Contentful Paint: 5.8 s
- Total Blocking Time: 650 ms
- Cumulative Layout Shift: 0.107
- Speed Index: 5.1 s

## Branch strategy

The performance work is isolated on:

`Audit/Performance-Optimization-2026-09`

This branch was created from:

`Audit/SEO-Technical-2026-09`

`Audit/SEO-Technical-2026-09` is intentionally left untouched as the deployment restore point.

## Initial technical findings

1. The global layout loads Bootstrap, jQuery, jQuery Validation, unobtrusive validation, Font Awesome, Google Fonts, lazy-load code and site JavaScript. Some of these resources may not be required on the public homepage.
2. The homepage Top Story image is eager-loaded, but it does not currently declare explicit intrinsic dimensions or `fetchpriority="high"`.
3. Many lower-priority homepage images use lazy loading, including Latest, Featured and category images.
4. Advertisement placeholders are present in the layout and homepage. Their final rendered heights should be verified to prevent layout shifts.
5. `site.css` contains Google Fonts `@import` statements in addition to Google Fonts links in `_Layout.cshtml`, which can duplicate font discovery and create unnecessary render-path work.
6. JavaScript contains several separate `DOMContentLoaded` handlers and analytics/event handlers. The main-thread impact should be measured before and after consolidation/defer changes.
7. The project already enables HTTPS response compression and static files; production CDN/cache/protocol headers still need verification.

## Work plan

Phase 1: LCP/FCP and critical rendering path.

Phase 2: main-thread/TBT reduction.

Phase 3: CLS stabilization for images, ads and dynamic components.

Phase 4: static asset, compression, CDN and browser-cache verification.

Phase 5: responsive image payload and below-the-fold image audit.

## Safety rule

Do not modify `Audit/SEO-Technical-2026-09` as part of this work. All performance changes must be made on `Audit/Performance-Optimization-2026-09` or subsequent performance-specific branches created from it.