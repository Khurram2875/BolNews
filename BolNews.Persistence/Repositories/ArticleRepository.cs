using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BolNews.Persistence.Repositories
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ArticleRepository> _logger;
        public ArticleRepository(AppDbContext context, ILogger<ArticleRepository> logger)
        {
             _context = context;
             _logger = logger;
        }

        public async Task<int> AddAsync(Article article)
        {
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();
            return article.Id;
        }

        public async Task UpdateAsync(Article article)
        {
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();
        }

        public async Task<Article?> FindByIdAsync(int id)
        {
            return await _context.Articles
            .Include(a => a.Author)
                .ThenInclude(a => a.User)

            .Include(a => a.Category)
            .Include(a => a.Reporter)

            .Include(a => a.ReviewerUser)

            .Include(a => a.FactCheckerUser)
            .Include(a => a.ArticleTags)
                .ThenInclude(a => a.Tag)
            .Include(a => a.FeaturedImageMetadata)
                .ThenInclude(a => a.FeaturedImageTags)
                    .ThenInclude(a => a.Tag)
            .Include(a => a.FeaturedMedia)

            .Include(a => a.DiscussionComments
                .Where(c => !c.IsDeleted))
                    .ThenInclude(c => c.User)

            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        }

        public async Task<Article?> FindPublishedByIdAsync(int id)
            => await _context.Articles
                .AsNoTracking()
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsPublished && !a.IsDeleted);

        public async Task<bool> SlugExistsAsync(string slug)
            => await _context.Articles.AnyAsync(a => a.Slug == slug);
        public async Task<Article?> FindBySlugAsync(string slug)
        {
            return await _context.Articles
                .AsNoTracking()
                .AsSplitQuery()
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(a => a.Tag)
                .Include(a => a.FeaturedImageMetadata)
                    .ThenInclude(a => a.FeaturedImageTags)
                        .ThenInclude(a => a.Tag)
                .FirstOrDefaultAsync(a =>
                    a.Slug == slug &&
                    !a.IsDeleted);
        }
        public async Task<PublicArticleData?> GetPublicArticleBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var query = _context.Articles
                .AsNoTracking()
                .AsSplitQuery()
                .Where(a =>
                    a.Slug == slug &&
                    !a.IsDeleted &&
                    a.IsPublished)
                .Select(a => new PublicArticleData
                {
                    Id = a.Id,
                    Title = a.Title,
                    Slug = a.Slug,

                    MetaTitle = a.MetaTitle,
                    MetaDescription = a.MetaDescription,

                    Summary = a.Summary,
                    Content = a.Content,

                    FeaturedImageThumb = a.FeaturedImageThumb,
                    FeaturedImageMedium = a.FeaturedImageMedium,
                    FeaturedImageLarge = a.FeaturedImageLarge,
                    FeaturedImageXl = a.FeaturedImageXl,

                    PublishedAt = a.PublishedAt,
                    UpdatedAt = a.UpdatedAt,

                    CategoryId = a.CategoryId,
                    CategoryName = a.Category.Name,
                    CategorySlug = a.Category.Slug,

                    AuthorId = a.AuthorId,
                    AuthorName = a.Author.Name,
                    AuthorSlug = a.Author.Slug,
                    AuthorImage = a.Author.ProfileImageUrl ?? string.Empty,

                    ReporterId = a.ReporterId,
                    ReporterName = a.Reporter != null
                        ? a.Reporter.Name
                        : null,
                    ReporterSourceName = a.Reporter != null
                        ? a.Reporter.SourceName
                        : null,

                    FeaturedImageAltText =
                        a.FeaturedImageMetadata != null
                            ? a.FeaturedImageMetadata.AltText
                            : null,

                    FeaturedImageCaption =
                        a.FeaturedImageMetadata != null
                            ? a.FeaturedImageMetadata.Caption
                            : null,

                    FeaturedImageCredit =
                        a.FeaturedImageMetadata != null
                            ? a.FeaturedImageMetadata.Credit
                            : null,

                    ArticleTags = a.ArticleTags
                        .Where(at => at.Tag != null)
                        .Select(at => new TagDto
                        {
                            Id = at.Tag.Id,
                            Name = at.Tag.Name,
                            Slug = at.Tag.Slug
                        })
                        .OrderBy(t => t.Name)
                        .ToList(),

                    ArticleTagIds = a.ArticleTags
                        .Select(at => at.TagId)
                        .ToList(),

                    FeaturedImageTags =
                        a.FeaturedImageMetadata != null
                            ? a.FeaturedImageMetadata.FeaturedImageTags
                                .Where(ft => ft.Tag != null)
                                .Select(ft => new TagDto
                                {
                                    Id = ft.Tag.Id,
                                    Name = ft.Tag.Name,
                                    Slug = ft.Tag.Slug
                                })
                                .OrderBy(t => t.Name)
                                .ToList()
                            : new List<TagDto>()
                });

            return await query.FirstOrDefaultAsync();
        }


        public async Task<List<Article>> GetAllAsync()
        {
            var result = await _context.Articles
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .Include(a => a.ReviewerUser)
                .Include(a => a.FactCheckerUser)
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return result;
        }


        public async Task<List<Article>> GetByAuthorIdAsync(int authorId, int page, int pageSize)
        => await _context.Articles
            .AsNoTracking()
            .Include(a => a.Category)
            .Where(a =>
                a.AuthorId == authorId &&
                a.IsPublished &&
                !a.IsDeleted)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        public async Task<List<Article>> GetByCategorySlugAsync(
            string categorySlug,
            int page,
            int pageSize)
        {
            if (page < 1)
                page = 1;

            if (pageSize <= 0)
                pageSize = 20;

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.Category.Slug == categorySlug &&
                    a.IsPublished == true &&
                    a.IsDeleted == false)
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Article>> GetCategoryArticlesAsync(
            string categorySlug,
            int skip,
            int take)
        {
            if (skip < 0)
                skip = 0;

            if (take <= 0)
                take = 20;

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.Category.Slug == categorySlug &&
                    a.IsPublished &&
                    !a.IsDeleted)
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Article>> GetByTagSlugAsync(string tagSlug, int page, int pageSize)
            => await _context.Articles
                .AsNoTracking()
                .Where(a => a.IsPublished == true &&
                            a.IsDeleted == false &&
                            a.ArticleTags.Any(at => at.Tag.Slug == tagSlug))
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<List<Article>> GetPublishedAsync(int count)
        {
            if (count <= 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.IsPublished == true &&
                    a.IsDeleted == false)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .ToListAsync();
        }

        public async Task<Article?> GetLatestPublishedAsync(IReadOnlyCollection<int>? excludedArticleIds = null)
            => await LatestPublishedQuery(excludedArticleIds)
                .FirstOrDefaultAsync();

        public async Task<List<Article>> GetLatestPublishedAsync(int count, IReadOnlyCollection<int>? excludedArticleIds = null)
            => await LatestPublishedQuery(excludedArticleIds)
                .Take(count)
                .ToListAsync();

        public async Task<List<Article>> GetByCategoryIdAsync(int categoryId, int count)
        {
            if (count <= 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.CategoryId == categoryId &&
                    a.IsPublished == true &&
                    a.IsDeleted == false)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .ToListAsync();
        }
        //updated GetRelatedArticlesAsync method to improve performance and reduce memory usage
        public async Task<List<Article>> GetRelatedArticlesAsync(
    int articleId,
    int categoryId,
    IReadOnlyCollection<int> tagIds,
    int count)
        {
            if (count <= 0)
                return new List<Article>();

            var validTagIds = tagIds?
                .Where(id => id > 0)
                .Distinct()
                .ToArray()
                ?? Array.Empty<int>();

            var query = _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.Id != articleId &&
                    a.IsPublished &&
                    !a.IsDeleted &&
                    (
                        a.CategoryId == categoryId ||
                        (
                            validTagIds.Length > 0 &&
                            a.ArticleTags.Any(at =>
                                validTagIds.Contains(at.TagId))
                        )
                    ));

            if (validTagIds.Length > 0)
            {
                query = query
                    .OrderByDescending(a =>
                        a.ArticleTags.Count(at =>
                            validTagIds.Contains(at.TagId)))
                    .ThenByDescending(a =>
                        a.CategoryId == categoryId)
                    .ThenByDescending(a => a.OverallScore)
                    .ThenByDescending(a => a.PublishedAt);
            }
            else
            {
                query = query
                    .OrderByDescending(a =>
                        a.CategoryId == categoryId)
                    .ThenByDescending(a => a.OverallScore)
                    .ThenByDescending(a => a.PublishedAt);
            }

            return await query
                .Take(count)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .AsSplitQuery()
                .ToListAsync();
        }
        //orignal code for GetRelatedArticlesAsync method
        //    public async Task<List<Article>> GetRelatedArticlesAsync(
        //int articleId,
        //int categoryId,
        //IReadOnlyCollection<int> tagIds,
        //int count)
        //    {
        //        if (count <= 0)
        //            return new List<Article>();

        //        var validTagIds = tagIds?
        //            .Where(id => id > 0)
        //            .Distinct()
        //            .ToArray()
        //            ?? Array.Empty<int>();

        //        var query = _context.Articles
        //            .AsNoTracking()
        //            .Where(a =>
        //                a.Id != articleId &&
        //                a.IsPublished &&
        //                !a.IsDeleted &&
        //                (
        //                    a.CategoryId == categoryId ||
        //                    (
        //                        validTagIds.Length > 0 &&
        //                        a.ArticleTags.Any(at =>
        //                            validTagIds.Contains(at.TagId))
        //                    )
        //                ));

        //        if (validTagIds.Length > 0)
        //        {
        //            query = query
        //                .OrderByDescending(a =>
        //                    a.ArticleTags.Count(at =>
        //                        validTagIds.Contains(at.TagId)))
        //                .ThenByDescending(a =>
        //                    a.CategoryId == categoryId)
        //                .ThenByDescending(a => a.OverallScore)
        //                .ThenByDescending(a => a.PublishedAt);
        //        }
        //        else
        //        {
        //            query = query
        //                .OrderByDescending(a =>
        //                    a.CategoryId == categoryId)
        //                .ThenByDescending(a => a.OverallScore)
        //                .ThenByDescending(a => a.PublishedAt);
        //        }

        //        return await query
        //            .Take(count)
        //            .Include(a => a.Category)
        //            .Include(a => a.Author)
        //            .Include(a => a.Reporter)
        //            .Include(a => a.ArticleTags)
        //                .ThenInclude(at => at.Tag)
        //            .ToListAsync();
        //    }

        public async Task<List<Article>> GetForCategoriesAsync(List<int> categoryIds, int count)
        {
            if (categoryIds == null ||
                categoryIds.Count == 0 ||
                count <= 0)
            {
                return new List<Article>();
            }

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    categoryIds.Contains(a.CategoryId) &&
                    a.IsPublished == true &&
                    a.IsDeleted == false)
                .OrderByDescending(a => a.PublishedAt)
                .Take(count)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .ToListAsync();
        }

        public async Task<List<Article>> GetPublishedSinceAsync(DateTime fromDate, int limit)
        {
            if (limit <= 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.PublishedAt >= fromDate &&
                    a.IsPublished == true &&
                    a.IsDeleted == false)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Article>> SearchAsync(string term, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<Article>();

            term = term.Trim();

            if (page < 1)
                page = 1;

            if (pageSize <= 0)
                pageSize = 20;

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.IsPublished &&
                    !a.IsDeleted &&
                    (
                        EF.Functions.Like(a.Title, $"%{term}%") ||
                        EF.Functions.Like(a.Content, $"%{term}%") ||
                        a.ArticleTags.Any(at =>
                            EF.Functions.Like(at.Tag.Name, $"%{term}%")) ||
                        (
                            a.FeaturedImageMetadata != null &&
                            (
                                EF.Functions.Like(
                                    a.FeaturedImageMetadata.AltText ?? "",
                                    $"%{term}%") ||

                                EF.Functions.Like(
                                    a.FeaturedImageMetadata.Caption ?? "",
                                    $"%{term}%") ||

                                a.FeaturedImageMetadata.FeaturedImageTags.Any(
                                    fit => EF.Functions.Like(
                                        fit.Tag.Name,
                                        $"%{term}%"))
                            )
                        )
                    ))
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .Include(a => a.FeaturedImageMetadata)
                    .ThenInclude(fim => fim.FeaturedImageTags)
                        .ThenInclude(fit => fit.Tag)
                .OrderByDescending(a => a.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Article>> GetTrendingCandidatesAsync(DateTime fromDate, int candidateLimit)
            => await _context.Articles.AsNoTracking()
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .Where(a => a.IsPublished == true && a.IsDeleted == false && a.PublishedAt >= fromDate)
                .OrderByDescending(a => a.ViewCount)
                .Take(candidateLimit)
                .ToListAsync();

        public async Task<List<Article>> GetTopByViewCountAsync(int count)
            => await _context.Articles.AsNoTracking()
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .Where(a => a.IsDeleted == false && a.IsPublished == true)
                .OrderByDescending(a => a.ViewCount)
                .Take(count)
                .ToListAsync();

        public async Task<List<Article>> GetLowPerformingAsync(DateTime since, int maxViews, int limit)
            => await _context.Articles.AsNoTracking()
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .Where(a => a.PublishedAt >= since && a.ViewCount < maxViews)
                .OrderByDescending(a => a.PublishedAt)
                .Take(limit)
                .ToListAsync();

        public async Task<int> CountAsync()
            => await _context.Articles.CountAsync(a => !a.IsDeleted);

        public async Task<int> CountPublishedSinceAsync(DateTime since)
            => await _context.Articles.CountAsync(a => a.PublishedAt >= since && !a.IsDeleted);

        public async Task<List<(DateTime Date, int Count)>> CountPerDayAsync(DateTime fromDate)
        {
            var data = await _context.Articles.AsNoTracking()
                .Where(a => a.PublishedAt >= fromDate && !a.IsDeleted)
                .GroupBy(a => a.PublishedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();
            return data.Select(x => (x.Date, x.Count)).ToList();
        }

        public async Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(DateTime fromDate)
            => await _context.Articles
                .Where(a => a.PublishedAt >= fromDate && a.IsDeleted == false && a.IsPublished == true)
                .Include(a => a.Category)
                .GroupBy(a => a.Category.Name)
                .Select(g => new CategoryPerformanceDto
                {
                    CategoryName = g.Key,
                    ArticleCount = g.Count(),
                    TotalViews = g.Sum(a => a.ViewCount),
                    AvgViewsPerArticle = g.Average(a => a.ViewCount)
                })
                .OrderByDescending(x => x.TotalViews)
                .ToListAsync();

        public async Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(DateTime fromDate)
            => await _context.Articles.AsNoTracking()
                .Where(a => a.PublishedAt >= fromDate && a.IsDeleted == false && a.IsPublished == true)
                .Include(a => a.Author)
                .GroupBy(a => a.Author.Name)
                .Select(g => new EditorPerformanceDto
                {
                    AuthorName = g.Key,
                    ArticleCount = g.Count(),
                    TotalViews = g.Sum(a => a.ViewCount),
                    AvgViewsPerArticle = g.Average(a => a.ViewCount)
                })
                .OrderByDescending(x => x.TotalViews)
                .ToListAsync();

        public async Task IncrementViewCountAsync(int articleId)
            => await _context.Articles
                .Where(a => a.Id == articleId)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1));

        public async Task BulkUpdateAsync(IEnumerable<Article> articles)
        {
            _context.Articles.UpdateRange(articles);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Article>> GetTopRankedPublishedAsync(int count)
        {
            if (count <= 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.IsDeleted == false &&
                    a.IsPublished == true)
                .OrderByDescending(a => a.OverallScore)
                .ThenByDescending(a => a.PublishedAt)
                .Take(count)
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .ToListAsync();
        }

        public async Task<List<Article>> GetTopRankedByCategoryAsync(int categoryId, int count)
        {
            if (count <= 0)
                return new List<Article>();

            return await _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.IsDeleted == false &&
                    a.IsPublished == true &&
                    a.CategoryId == categoryId)
                .OrderByDescending(a => a.OverallScore)
                .ThenByDescending(a => a.PublishedAt)
                .Take(count)
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .ToListAsync();
        }
        public async Task<List<Article>> GetByWorkflowStatusAsync(ArticleWorkflowStatus status)
        {
            return await _context.Articles.AsNoTracking()
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .Where(a =>
                    !a.IsDeleted &&
                    a.WorkflowStatus == status)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        public async Task<List<Article>> GetEditorialQueueAsync(params ArticleWorkflowStatus[] statuses)
        {
            return await _context.Articles.AsNoTracking()
             .Include(a => a.Author)
             .ThenInclude(a => a.User)
             .Include(a => a.Category)
             .Include(a => a.Reporter)
             .Include(a => a.ReviewerUser)
             .Include(a => a.FactCheckerUser)
             .Where(a =>
                 !a.IsDeleted &&
                 statuses.Contains(a.WorkflowStatus))
             .OrderByDescending(a => a.CreatedAt)
             .ToListAsync();
        }
        public async Task<List<Article>> GetActiveWorkflowArticlesAsync()
        {
            return await _context.Articles
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)

                .Include(a => a.Category)
                .Include(a => a.Reporter)

                .Where(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.Submitted ||
                    a.WorkflowStatus == ArticleWorkflowStatus.UnderReview ||
                    a.WorkflowStatus == ArticleWorkflowStatus.FactCheckPending ||
                    a.WorkflowStatus == ArticleWorkflowStatus.Approved)
                .ToListAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<List<Article>> GetDueScheduledArticlesAsync(DateTime utcNow)
        {

            return await _context.Articles
                .Include(a => a.Author)
                    .ThenInclude(a => a.User)
                .Include(a => a.Category)
                .Include(a => a.Reporter)
                .Where(a =>
                    !a.IsDeleted &&
                    !a.IsPublished &&
                    a.WorkflowStatus == ArticleWorkflowStatus.Approved &&
                    (
                        (a.ScheduledPublishAt.HasValue &&
                         a.ScheduledPublishAt <= utcNow)
                        ||
                        (a.EmbargoUntil.HasValue &&
                         a.EmbargoUntil <= utcNow)
                    ))
                .ToListAsync();
        }

        private IQueryable<Article> LatestPublishedQuery(IReadOnlyCollection<int>? excludedArticleIds)
        {
            var excludedCategories = new[]
            {"Lifestyle", "Health"};

            var query = _context.Articles
                .AsNoTracking()
                .Where(a =>
                    a.IsPublished == true &&
                    a.IsDeleted == false &&
                    !excludedCategories.Contains(a.Category.Name));

            if (excludedArticleIds is { Count: > 0 })
            {
                var excludedIds = excludedArticleIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToArray();

                if (excludedIds.Length > 0)
                {
                    query = query.Where(a => !excludedIds.Contains(a.Id));
                }
            }

            return query
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishedAt);
        }

        public async Task<List<Article>> SearchPublishedAsync(string search, int take = 20)
        {
            return await _context.Articles
                .Where(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.Published &&
                    a.Title.Contains(search))
                .OrderByDescending(a => a.PublishedAt)
                .Take(take)
                .ToListAsync();
        }
        public async Task<List<Article>> GetDeletedAsync()
        {
            var result = await _context.Articles
             .IgnoreQueryFilters()
             .Where(x => x.IsDeleted)
             .Include(a => a.Author)
                 .ThenInclude(a => a.User)
             .Include(a => a.Category)
             .Include(a => a.Reporter)
             .Include(a => a.ReviewerUser)
             .Include(a => a.FactCheckerUser)
             .OrderByDescending(a => a.UpdatedAt)
             .ToListAsync();

            return result;
        }
        public async Task<Dictionary<int, List<Article>>> GetLatestArticlesForCategoriesAsync(
     IReadOnlyCollection<int> categoryIds,
     int count)
        {
            if (categoryIds == null || categoryIds.Count == 0 || count <= 0)
                return new Dictionary<int, List<Article>>();

            var ids = categoryIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            if (ids.Length == 0)
                return new Dictionary<int, List<Article>>();

            var query = _context.Articles
                .AsNoTracking()
                .Where(a =>
                    ids.Contains(a.CategoryId) &&
                    a.IsPublished &&
                    !a.IsDeleted)
                .Include(a => a.Category)
                .Include(a => a.Author)
                .Include(a => a.Reporter)
                .OrderByDescending(a => a.PublishedAt);

            Console.WriteLine("========== CATEGORY ARTICLES SQL ==========");
            Console.WriteLine(query.ToQueryString());
            Console.WriteLine("==========================================");

            var articles = await query.ToListAsync();

            return articles
                .GroupBy(a => a.CategoryId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Take(count * 3).ToList());
        }
        public async Task<Article?> GetBySourceAsync(string sourceSystem, string sourceId)
        {
            return await _context.Articles
                .FirstOrDefaultAsync(a =>
                    a.SourceSystem == sourceSystem &&
                    a.SourceId == sourceId);
        }
        public async Task<Article?> FindBySourceAsync(string sourceSystem, string sourceId)
        {
            return await _context.Articles
                .FirstOrDefaultAsync(a =>
                    a.SourceSystem == sourceSystem &&
                    a.SourceId == sourceId &&
                    !a.IsDeleted);
        }
        public async Task<List<Article>> GetWordPressArticlesWithMissingFeaturedImagesAsync()
        {
            return await _context.Articles
                .Where(a =>
                    a.SourceSystem == "WordPress" &&
                    !a.IsDeleted &&
                    string.IsNullOrEmpty(a.FeaturedImageThumb))
                .OrderBy(a => a.Id)
                .ToListAsync();
        }
    }
}
