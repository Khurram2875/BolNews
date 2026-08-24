using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Interfaces.Scoring;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BolNews.Application.Services
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _repo;
        private readonly IMemoryCache _cache;
        private readonly IArticleScoringService _articleScoringService;
        private readonly IArticleRevisionService _articleRevisionService;
        private readonly IAuthorService _authorService;
        private readonly INotificationService _notificationService;
        private readonly IWorkflowTransitionService _workflowTransitionService;
        private readonly IEditorialAssignmentService _editorialAssignmentService;
        private readonly ISlaService _slaService;
        private readonly IEditorialPlacementRepository? _editorialPlacementRepository;
        private readonly ITagService? _tagService;
        private readonly ILogger<ArticleService> _logger;

        #region Role and other private helpers
        private bool IsAdmin(IList<string> roles)
    => roles.Contains(Roles.Admin);

        private bool IsEditor(IList<string> roles)
            => roles.Contains(Roles.Editor);

        private bool IsSubEditor(IList<string> roles)
            => roles.Contains(Roles.SubEditor);

        private bool IsAuthor(IList<string> roles)
            => roles.Contains(Roles.Author);
        private void ValidateArticleEditPermission(
    Article article,
    string currentUserId,
    IList<string> roles)
        {
            // Editorial roles have full edit authority
            if (IsAdmin(roles) || IsEditor(roles) || IsSubEditor(roles))
                return;

            // Author rules
            if (IsAuthor(roles))
            {
                var ownsArticle = article.Author?.UserId == currentUserId;

                if (!ownsArticle)
                {
                    throw new UnauthorizedAccessException(
                        "Authors can only edit their own articles.");
                }

                var editableStatuses = new[]
                {
            ArticleWorkflowStatus.Draft,
            ArticleWorkflowStatus.Rejected
        };

                if (!editableStatuses.Contains(article.WorkflowStatus))
                {
                    throw new UnauthorizedAccessException(
                        "This article is currently in editorial workflow and cannot be edited.");
                }

                return;
            }

            throw new UnauthorizedAccessException("Access denied.");
        }
        private void ApplyEditorialControls(Article article, ArticleDto dto, IList<string> roles)
        {
            if (IsAdmin(roles) || IsEditor(roles))
            {
                article.IsEditorsPick = dto.IsEditorsPick;
                article.EditorialPriority = dto.EditorialPriority;
                article.IsFactChecked = dto.IsFactChecked;
                article.IsPublished = dto.IsPublished;

                if (dto.IsPublished)
                {
                    article.IsPublished = true;
                    article.WorkflowStatus = ArticleWorkflowStatus.Published;

                    if (!article.PublishedAt.HasValue)
                        article.PublishedAt = DateTime.UtcNow;
                }
                else
                {
                    article.IsPublished = false;

                    if (article.WorkflowStatus == ArticleWorkflowStatus.Published)
                        article.WorkflowStatus = ArticleWorkflowStatus.Draft;
                }

                if (!dto.IsPublished)
                    article.PublishedAt = null;

                return;
            }

            if (IsSubEditor(roles))
            {
                article.IsFactChecked = dto.IsFactChecked;
                article.IsPublished = dto.IsPublished;

                article.EditorialPriority =
                    Math.Min(dto.EditorialPriority, 1);

                article.IsEditorsPick = false;

                if (dto.IsPublished && !article.PublishedAt.HasValue)
                    article.PublishedAt = DateTime.UtcNow;

                if (dto.IsPublished)
                    article.WorkflowStatus = ArticleWorkflowStatus.Published;

                if (!dto.IsPublished)
                    article.PublishedAt = null;

                return;
            }

            if (IsAuthor(roles))
            {
                article.IsEditorsPick = false;
                article.EditorialPriority = 0;
                article.IsFactChecked = false;
                article.IsPublished = false;
                article.PublishedAt = null;
            }
        }
        #endregion
        public ArticleService(IArticleRepository repo, IMemoryCache cache, IArticleScoringService articleScoringService, IArticleRevisionService articleRevisionService, IAuthorService authorService, INotificationService notificationService, IWorkflowTransitionService workflowTransitionService, IEditorialAssignmentService editorialAssignmentService, ISlaService slaService, IEditorialPlacementRepository? editorialPlacementRepository = null, ITagService? tagService = null, ILogger<ArticleService>? logger = null)
        {
            _repo = repo;
            _cache = cache;
            _articleScoringService = articleScoringService;
            _articleRevisionService = articleRevisionService;
            _authorService = authorService;
            _notificationService = notificationService;
            _workflowTransitionService = workflowTransitionService;
            _editorialAssignmentService = editorialAssignmentService;
            _slaService = slaService;
            _editorialPlacementRepository = editorialPlacementRepository;
            _tagService = tagService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
        }

        public async Task<int> CreateAsync(ArticleDto dto, string currentUserId,IList<string> roles)
        {
            int authorId = dto.AuthorId;
            if (IsAuthor(roles))
            {
                var author = await _authorService.GetAuthorByUserId(currentUserId);

                if (author == null)
                    throw new UnauthorizedAccessException(
                        "Author profile not found.");

                authorId = author.Id;
            }
            else if (IsSubEditor(roles))
            {
                var author = await _authorService.GetAuthorByUserId(currentUserId);

                if (author == null)
                    throw new UnauthorizedAccessException(
                        "Author profile not found.");

                authorId = author.Id;
            }
            var workflowStatus = DetermineInitialWorkflowStatus(dto, roles);
            var article = new Article
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Summary = dto.Summary,
                Content = dto.Content,

                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,

                FeaturedImageThumb = dto.FeaturedImageThumb,
                FeaturedImageMedium = dto.FeaturedImageMedium,
                FeaturedImageLarge = dto.FeaturedImageLarge,
                FeaturedImageXl = dto.FeaturedImageXl,
                FeaturedMediaId = dto.FeaturedMediaId,
                
                CategoryId = dto.CategoryId,
                AuthorId = authorId,
                ReporterId = dto.ReporterId,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId,
                WorkflowStatus = workflowStatus,
            };
            if (workflowStatus == ArticleWorkflowStatus.Submitted)
            {
                article.SubmittedAt = DateTime.UtcNow;
            }
            ApplyEditorialControls(article, dto, roles);

            await _articleScoringService.CalculateScoresAsync(article);

            var articleId = await _repo.AddAsync(article);

            if (_tagService != null)
            {
                await _tagService.ReplaceArticleTagsAsync(articleId, dto.ArticleTagsInput, currentUserId);
                await _tagService.ReplaceFeaturedImageTagsAsync(
                    articleId,
                    dto.FeaturedImageTagsInput,
                    dto.FeaturedImageAltText,
                    dto.FeaturedImageCaption,
                    dto.FeaturedImageCredit,
                    currentUserId);
            }

            return articleId;
        }

        public async Task UpdateAsync(ArticleDto dto, string currentUserId, IList<string> roles, string? changeReason = null)
        {
            var article = await _repo.FindByIdAsync(dto.Id);
            //if (article == null) return;
            if (article == null)
                throw new InvalidOperationException(
                    $"Article with id {dto.Id} was not found.");

            ValidateArticleEditPermission(article, currentUserId, roles);

            await _articleRevisionService.CreateSnapshotAsync(
                    article,
                    currentUserId,
                    workflowState: article.IsPublished ? "PublishedUpdate" : "DraftUpdate",
                    changeReason: changeReason
                );
            article.Title = dto.Title;
            article.Slug = dto.Slug;
            article.MetaTitle = dto.MetaTitle;
            article.MetaDescription = dto.MetaDescription;
            article.Summary = dto.Summary;
            article.Content = dto.Content;

            article.FeaturedImageThumb = dto.FeaturedImageThumb;
            article.FeaturedImageMedium = dto.FeaturedImageMedium;
            article.FeaturedImageLarge = dto.FeaturedImageLarge;
            article.FeaturedImageXl = dto.FeaturedImageXl;
            article.FeaturedMediaId = dto.FeaturedMediaId;
            article.ScheduledPublishAt = dto.ScheduledPublishAt;
            article.EmbargoUntil = dto.EmbargoUntil;

            article.CategoryId = dto.CategoryId;
            article.ReporterId = dto.ReporterId;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;

            

            ApplyEditorialControls(article, dto, roles);
            // AUTHOR SUBMISSION WORKFLOW
            if (IsAuthor(roles) && dto.SubmitForReview)
            {
                article.WorkflowStatus = ArticleWorkflowStatus.Submitted;
                article.WorkflowComment = null;

                // SLA lifecycle reset (new submission cycle)
                article.SubmittedAt = DateTime.UtcNow;
                article.ReviewStartedAt = null;
                article.FactCheckStartedAt = null;
                article.ApprovedAt = null;

                // notify assigned reviewer if already assigned
                if (!string.IsNullOrWhiteSpace(article.ReviewerUserId))
                {
                    await _notificationService.NotifyAsync(
                        article.ReviewerUserId,
                        "Article Submitted",
                        $"Article '{article.Title}' has been submitted for review.",
                        $"/Admin/Articles/Edit/{article.Id}");
                }
            }
            await _articleScoringService.CalculateScoresAsync(article);

            
            try
            {
                await _repo.UpdateAsync(article);

                if (_tagService != null)
                {
                    await _tagService.ReplaceArticleTagsAsync(article.Id, dto.ArticleTagsInput, currentUserId);
                    await _tagService.ReplaceFeaturedImageTagsAsync(
                        article.Id,
                        dto.FeaturedImageTagsInput,
                        dto.FeaturedImageAltText,
                        dto.FeaturedImageCaption,
                        dto.FeaturedImageCredit,
                        currentUserId);
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException(
                    "This article was modified by another user. Please reload and try again.");
            }

        }

        public async Task DeleteAsync(int id, string deletedByUserId, string? reason = null)
        {
            var article = await _repo.FindByIdAsync(id);
            if (article == null) return;

            // Snapshot the article's state at the moment of deletion — reuses the same
            // revision history already surfaced via the "Changes" link in the article list.
            await _articleRevisionService.CreateSnapshotAsync(
                article,
                deletedByUserId,
                workflowState: $"{article.WorkflowStatus} -> Deleted",
                changeReason: reason ?? "No reason provided.");

            article.IsDeleted = true;
            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = deletedByUserId;

            await _repo.UpdateAsync(article);
        }

        public async Task<ArticleDto?> GetByIdAsync(int id)
        {
            var a = await _repo.FindByIdAsync(id);
            if (a == null) return null;
            return new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                Content = a.Content,
                MetaTitle = a.MetaTitle,
                MetaDescription = a.MetaDescription,
                FeaturedImageXl = a.FeaturedImageXl,
                FeaturedImageLarge = a.FeaturedImageLarge,
                FeaturedImageMedium = a.FeaturedImageMedium,
                FeaturedImageThumb = a.FeaturedImageThumb,
                CategoryId = a.CategoryId,
                AuthorId = a.AuthorId,
                AuthorName= a.Author.Name,
                ReporterId = a.ReporterId,
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                WorkflowStatus = a.WorkflowStatus,
                WorkflowComment = a.WorkflowComment,
                IsEditorsPick = a.IsEditorsPick,
                EditorialPriority = a.EditorialPriority,
                IsFactChecked = a.IsFactChecked,
                ScheduledPublishAt = a.ScheduledPublishAt,
                EmbargoUntil = a.EmbargoUntil,
                FeaturedImageAltText = a.FeaturedImageMetadata?.AltText,
                FeaturedImageCaption = a.FeaturedImageMetadata?.Caption,
                FeaturedImageCredit = a.FeaturedImageMetadata?.Credit,
                FeaturedMediaId = a.FeaturedMediaId,
                ArticleTags = a.ArticleTags
                    .Select(at => new TagDto
                    {
                        Id = at.Tag.Id,
                        Name = at.Tag.Name,
                        Slug = at.Tag.Slug
                    })
                    .OrderBy(t => t.Name)
                    .ToList(),
                FeaturedImageTags = a.FeaturedImageMetadata?.FeaturedImageTags
                    .Select(ft => new TagDto
                    {
                        Id = ft.Tag.Id,
                        Name = ft.Tag.Name,
                        Slug = ft.Tag.Slug
                    })
                    .OrderBy(t => t.Name)
                    .ToList() ?? new List<TagDto>()
            };
        }

        public async Task<IEnumerable<ArticleDto>> GetAllAsync(string userId, IList<string> roles)
        {
            var all = await _repo.GetAllAsync();
            IEnumerable<Article> filtered;
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor) || roles.Contains(Roles.SubEditor))
                filtered = all;
            else if (roles.Contains(Roles.Author))
                filtered = all.Where(a => a.Author?.UserId == userId);
            else
                return Enumerable.Empty<ArticleDto>();

            return filtered.Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                Content = a.Content,
                FeaturedImageThumb = a.FeaturedImageThumb,
                FeaturedImageMedium = a.FeaturedImageMedium,
                FeaturedImageLarge = a.FeaturedImageLarge,
                AuthorId = a.AuthorId,
                ReporterId = a.ReporterId,
                CategoryId = a.CategoryId,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                WorkflowStatus = a.WorkflowStatus,
                ReviewerName = a.ReviewerUser?.FullName,
                FactCheckerName = a.FactCheckerUser?.FullName,
                WorkflowComment = a.WorkflowComment,
                IsEditorsPick = a.IsEditorsPick,
                EditorialPriority = a.EditorialPriority,
                IsFactChecked = a.IsFactChecked,
                AuthorName = a.Author?.User?.FullName,
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                CategoryName = a.Category?.Name,
                CategorySlug=a.Category?.Slug
            });
        }

        public async Task UpdateImagesAsync(int id, string thumb, string medium, string large, string xl)
        {
            var article = await _repo.FindByIdAsync(id);
            if (article == null) return;
            article.FeaturedImageThumb = thumb;
            article.FeaturedImageMedium = medium;
            article.FeaturedImageLarge = large;
            article.FeaturedImageXl = xl;
            await _repo.UpdateAsync(article);
        }

        public async Task<string> GenerateUniqueSlugAsync(string title)
        {
            var baseSlug = SlugHelper.GenerateSlug(title);
            var slug = baseSlug;
            int count = 1;
            while (await _repo.SlugExistsAsync(slug))
                slug = $"{baseSlug}-{count++}";
            return slug;
        }

        public async Task<Article> GetBySlugAsync(string slug) => await _repo.FindBySlugAsync(slug);
        public async Task<PublicArticleData?> GetPublicArticleBySlugAsync(string slug)
        {
            return await _repo.GetPublicArticleBySlugAsync(slug);
        }
        public async Task<List<Article>> GetByCategorySlugAsync(string categorySlug, int page) => await _repo.GetByCategorySlugAsync(categorySlug, page, 10);
        public async Task<List<Article>> GetCategoryArticlesAsync(string categorySlug, int skip, int take) =>
            await _repo.GetCategoryArticlesAsync(categorySlug, skip, take);
        public async Task<List<Article>> GetByTagSlugAsync(string tagSlug, int page = 1, int pageSize = 20) => await _repo.GetByTagSlugAsync(tagSlug, page, pageSize);

        public async Task<List<Article>> GetRelatedArticlesAsync(int categoryId, int excludeId, List<int> tagIds, int count = 5)
        {
            string key = $"related_{categoryId}_{excludeId}";

            if (_cache.TryGetValue(key, out List<Article>? cached))
                return cached!;

            var result = await _repo.GetRelatedArticlesAsync(
                excludeId,
                categoryId,
                tagIds,
                count);

            if (result.Count < count)
            {
                var existingIds = result
                    .Select(a => a.Id)
                    .Append(excludeId)
                    .ToHashSet();

                var fallback = await _repo.GetByCategoryIdAsync(
                    categoryId,
                    count + 1);

                result.AddRange(
                    fallback
                        .Where(a => !existingIds.Contains(a.Id))
                        .Take(count - result.Count));
            }

            _cache.Set(
                key,
                result,
                TimeSpan.FromMinutes(5));

            return result;
        }

        public async Task<List<Article>> GetLatestArticlesAsync(int count = 5) => await _repo.GetPublishedAsync(count);
        public async Task<List<Article>> GetAllPublishedAsync() => await _repo.GetPublishedAsync(int.MaxValue);
        public async Task<Article?> GetTopStoryAsync()
        {
            if (_editorialPlacementRepository == null)
            {
                return await _repo.GetLatestPublishedAsync();
            }

            var pinnedTopStory = await _editorialPlacementRepository.GetActivePlacementAsync(EditorialPlacementKeys.HomepageTopStory);

            if (pinnedTopStory?.Article is { IsPublished: true, IsDeleted: false } article)
            {
                return article;
            }

            return await _repo.GetLatestPublishedAsync();
        }

        public async Task<List<Article>> GetSecondaryStoriesAsync(int count = 20)
        {
            var selectedArticles = new List<Article>();
            var excludedArticleIds = new HashSet<int>();

            var topStory = await GetTopStoryAsync();
            if (topStory != null)
            {
                excludedArticleIds.Add(topStory.Id);
            }

            if (_editorialPlacementRepository != null)
            {
                var pinnedSecondaryStories = await _editorialPlacementRepository
                    .GetActivePlacementsWithArticlesAsync(EditorialPlacementKeys.HomepageSecondaryStory);

                foreach (var placement in pinnedSecondaryStories)
                {
                    if (selectedArticles.Count >= count)
                    {
                        break;
                    }

                    if (placement.Article is not { IsPublished: true, IsDeleted: false } article ||
                        excludedArticleIds.Contains(article.Id))
                    {
                        continue;
                    }

                    selectedArticles.Add(article);
                    excludedArticleIds.Add(article.Id);
                }
            }

            var remainingCount = count - selectedArticles.Count;
            if (remainingCount <= 0)
            {
                return selectedArticles;
            }

            var fallbackArticles = await _repo.GetLatestPublishedAsync(remainingCount, excludedArticleIds);
            selectedArticles.AddRange(fallbackArticles);

            return selectedArticles;
        }
        public async Task<List<Article>> GetArticlesByCategoryAsync(int catId, int n) => await _repo.GetByCategoryIdAsync(catId, n);
        public async Task<List<Article>> GetBreakingNewsAsync(int count = 5) => await _repo.GetPublishedAsync(count);
        public async Task<List<Article>> SearchAsync(string q, int page, int pageSize) => await _repo.SearchAsync(q.Trim(), page, pageSize);
        public async Task IncrementViewCountAsync(int articleId) => await _repo.IncrementViewCountAsync(articleId);

        //public async Task<Dictionary<int, List<Article>>> GetArticlesForCategoriesAsync(List<int> categoryIds, int count)
        //{
        //    if (categoryIds == null ||
        //        categoryIds.Count == 0 ||
        //        count <= 0)
        //    {
        //        return new Dictionary<int, List<Article>>();
        //    }

        //    var ids = categoryIds
        //        .Where(id => id > 0)
        //        .Distinct()
        //        .ToList();

        //    if (ids.Count == 0)
        //        return new Dictionary<int, List<Article>>();

        //    var articles = await _repo.GetForCategoriesAsync(ids, count);

        //    var result = ids.ToDictionary(
        //        id => id,
        //        _ => new List<Article>());

        //    foreach (var article in articles)
        //    {
        //        if (!result.TryGetValue(article.CategoryId, out var list))
        //            continue;

        //        if (list.Count < count)
        //            list.Add(article);
        //    }

        //    return result;
        //}

        public async Task<Dictionary<int, List<Article>>> GetArticlesForCategoriesAsync(List<int> categoryIds, int count)
        {
            return await _repo.GetLatestArticlesForCategoriesAsync(
                categoryIds,
                count);
        }

        public async Task<List<Article>> GetTrendingAsync(int count = 5, string type = "week")
        {
            var fromDate = type switch
            {
                "today" => DateTime.UtcNow.AddDays(-1),
                "week" => DateTime.UtcNow.AddDays(-7),
                "month" => DateTime.UtcNow.AddDays(-30),
                _ => DateTime.UtcNow.AddDays(-7)
            };
            var candidates = await _repo.GetTrendingCandidatesAsync(fromDate, candidateLimit: 500);
            var now = DateTime.UtcNow;
            return candidates
                .Select(a => (a, score: a.ViewCount + 200.0 / (1 + (now - a.PublishedAt!.Value).TotalHours)))
                .OrderByDescending(x => x.score).Take(count).Select(x => x.a).ToList();
        }

        public async Task<List<Article>> GetLatestPublishedAsync(DateTime fromDate, int limit) => await _repo.GetPublishedSinceAsync(fromDate, limit);

        public async Task<List<Article>> GetRecentArticlesAsync(int hours = 48)
        {
            var articles = await _repo.GetPublishedSinceAsync(DateTime.UtcNow.AddHours(-hours), 200);
            return articles.OrderByDescending(a => a.ViewCount).ThenByDescending(a => a.PublishedAt).ToList();
        }

        public async Task<int> GetTotalArticlesAsync() => await _repo.CountAsync();
        public async Task<int> GetTodayArticlesCountAsync() => await _repo.CountPublishedSinceAsync(DateTime.UtcNow.Date);
        public async Task<List<Article>> GetTopArticlesAsync(int n) => await _repo.GetTopByViewCountAsync(n);
        public async Task<List<Article>> GetLowPerformingArticlesAsync() => await _repo.GetLowPerformingAsync(DateTime.UtcNow.AddDays(-2), 50, 10);
        public async Task<List<(DateTime date, int count)>> GetArticlesPerDayAsync(int days = 7) => await _repo.CountPerDayAsync(DateTime.UtcNow.Date.AddDays(-days));
        public async Task<List<CategoryPerformanceDto>> GetCategoryPerformanceAsync(int days = 7) => await _repo.GetCategoryPerformanceAsync(DateTime.UtcNow.AddDays(-days));
        public async Task<List<EditorPerformanceDto>> GetEditorPerformanceAsync(int days = 7) => await _repo.GetEditorPerformanceAsync(DateTime.UtcNow.AddDays(-days));

        public async Task<bool> CanEditAsync(int articleId, string userId, IList<string> roles)
        {
            var article = await _repo.FindByIdAsync(articleId);

            if (article == null)
                return false;

            if (roles.Contains(Roles.Admin) ||
                roles.Contains(Roles.Editor) ||
                roles.Contains(Roles.SubEditor))
            {
                return true;
            }

            
            if (roles.Contains(Roles.Author))
            {
                return article.Author?.UserId == userId &&
                     (
                       article.WorkflowStatus == ArticleWorkflowStatus.Draft ||
                       article.WorkflowStatus == ArticleWorkflowStatus.Rejected
                    );
            }
            return false;
        }

        public async Task<bool> CanDeleteAsync(int articleId, string userId, IList<string> roles)
        {
            if (roles.Contains(Roles.Admin) || roles.Contains(Roles.Editor)) return true;
            if (roles.Contains(Roles.Author)) return (await _repo.FindByIdAsync(articleId))?.Author?.UserId == userId;
            return false;
        }

        public async Task<List<Article>> GetByAuthorAsync(int authorId, int page = 1, int pageSize = 20)
            => await _repo.GetByAuthorIdAsync(authorId, page, pageSize);

        public async Task<IEnumerable<ArticleDto>> GetTopRankedPublishedAsync(int count)
        {
            var articles = await _repo.GetTopRankedPublishedAsync(count);

            return articles.Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                Content = a.Content,
                FeaturedImageThumb = a.FeaturedImageThumb,
                FeaturedImageMedium = a.FeaturedImageMedium,
                FeaturedImageLarge = a.FeaturedImageLarge,
                FeaturedImageXl = a.FeaturedImageXl,
                AuthorId = a.AuthorId,
                ReporterId = a.ReporterId,
                CategoryId = a.CategoryId,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                IsEditorsPick = a.IsEditorsPick,
                EditorialPriority = a.EditorialPriority,
                IsFactChecked = a.IsFactChecked,
                AuthorName = a.Author?.User?.FullName,
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                CategoryName = a.Category?.Name,
                CategorySlug= a.Category?.Slug
            });
        }
        public async Task<IEnumerable<ArticleDto>> GetTopRankedByCategoryAsync(int categoryId, int count)
        {
            var articles = await _repo.GetTopRankedByCategoryAsync(categoryId, count);

            return articles.Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                Content = a.Content,
                FeaturedImageThumb = a.FeaturedImageThumb,
                FeaturedImageMedium = a.FeaturedImageMedium,
                FeaturedImageLarge = a.FeaturedImageLarge,
                FeaturedImageXl = a.FeaturedImageXl,
                AuthorId = a.AuthorId,
                ReporterId = a.ReporterId,
                CategoryId = a.CategoryId,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                IsEditorsPick = a.IsEditorsPick,
                EditorialPriority = a.EditorialPriority,
                IsFactChecked = a.IsFactChecked,
                AuthorName = a.Author?.User?.FullName,
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                CategoryName = a.Category?.Name
            });
        }
        private ArticleWorkflowStatus DetermineInitialWorkflowStatus(ArticleDto dto, IList<string> roles)
        {
            if (IsAuthor(roles))
            {
                return dto.SubmitForReview
                    ? ArticleWorkflowStatus.Submitted
                    : ArticleWorkflowStatus.Draft;
            }

            if (IsSubEditor(roles))
            {
                return dto.IsPublished
                    ? ArticleWorkflowStatus.Published
                    : ArticleWorkflowStatus.UnderReview;
            }

            if (IsEditor(roles) || IsAdmin(roles))
            {
                return dto.IsPublished
                    ? ArticleWorkflowStatus.Published
                    : ArticleWorkflowStatus.Approved;
            }

            return ArticleWorkflowStatus.Draft;
        }
        private bool IsFactChecker(IList<string> roles)
        {
            return roles.Contains(Roles.Factchecker);
        }
        public async Task<List<EditorialQueueDto>> GetEditorialQueueAsync(IList<string> roles)
        {
            if (!(IsAdmin(roles) ||
                  IsEditor(roles) ||
                  IsSubEditor(roles) ||
                  IsFactChecker(roles)))
            {
                return new List<EditorialQueueDto>();
            }

            var articles = await _repo.GetEditorialQueueAsync(
                ArticleWorkflowStatus.Submitted,
                ArticleWorkflowStatus.UnderReview,
                ArticleWorkflowStatus.FactCheckPending,
                ArticleWorkflowStatus.Approved);

            return articles.Select(a => new EditorialQueueDto
            {
                Id = a.Id,
                Title = a.Title,
                AuthorName = a.Author?.Name ?? "",
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                CategoryName = a.Category?.Name ?? "",
                CreatedAt = a.CreatedAt,
                WorkflowStatus = a.WorkflowStatus,
                OverallScore = a.OverallScore,
                IsFactChecked = a.IsFactChecked,
                IsPublished = a.IsPublished,
                ReviewerName = a.ReviewerUser?.FullName,
                FactCheckerName = a.FactCheckerUser?.FullName,
                ReviewerUserId = a.ReviewerUserId,
                FactCheckerUserId = a.FactCheckerUserId,
                SlaStatus = _slaService.Evaluate(a),
                ScheduledPublishAt = a.ScheduledPublishAt,
                EmbargoUntil = a.EmbargoUntil
            }).ToList();
        }
        public async Task TransitionWorkflowAsync(int articleId, ArticleWorkflowStatus targetStatus, string currentUserId, IList<string> roles, string? reason = null, DateTime? scheduledPublishAt = null, DateTime? embargoUntil = null)
        {
            var article = await _repo.FindByIdAsync(articleId);

            if (article == null)
                throw new InvalidOperationException(
                    $"Article {articleId} not found.");

            article.ScheduledPublishAt = scheduledPublishAt;
            article.EmbargoUntil = embargoUntil;
            await _workflowTransitionService.ExecuteTransitionAsync(
                article, targetStatus, currentUserId, roles, reason);

            await _repo.UpdateAsync(article);
        }
        public async Task AssignReviewerAsync(int articleId,string reviewerUserId,string currentUserId,IList<string> roles)
        {
            var article = await _repo.FindByIdAsync(articleId);

            if (article == null)
            {
                throw new InvalidOperationException(
                    $"Article {articleId} not found.");
            }

            await _editorialAssignmentService.AssignReviewerAsync(article, reviewerUserId, currentUserId, roles);
            await _repo.UpdateAsync(article);
        }
        public async Task AssignFactCheckerAsync(int articleId,string factCheckerUserId,string currentUserId,IList<string> roles)
        {
            var article = await _repo.FindByIdAsync(articleId);

            if (article == null)
            {
                throw new InvalidOperationException(
                    $"Article {articleId} not found.");
            }

            await _editorialAssignmentService.AssignFactCheckerAsync(article, factCheckerUserId, currentUserId, roles);
            await _repo.UpdateAsync(article);
        }
        public async Task<Article?> GetEntityByIdAsync(int id)
        {
            return await _repo.FindByIdAsync(id);
        }
        public async Task CancelScheduleAsync(int articleId, string currentUserId, IList<string> roles)
        {
            var article = await _repo.FindByIdAsync(articleId);

            if (article == null)
                throw new InvalidOperationException("Article not found.");

            if (!(roles.Contains(Roles.Admin) ||
                  roles.Contains(Roles.Editor) ||
                  roles.Contains(Roles.SubEditor)))
            {
                throw new UnauthorizedAccessException();
            }

            article.ScheduledPublishAt = null;
            article.EmbargoUntil = null;

            article.UpdatedAt = DateTime.UtcNow;
            article.UpdatedBy = currentUserId;

            await _repo.SaveChangesAsync();
        }

        public async Task<List<Article>> SearchArticlesAsync(string search, int take = 20)
        {
            return await _repo.SearchPublishedAsync(search, take);
        }
        public async Task<List<Article>> GetLatestArticlesForCategoriesAsync(List<int> categoryIds, int count)
        {
            // GetForCategoriesAsync already returns published articles for these
            // category ids, ordered by PublishedAt descending (see GetArticlesForCategoriesAsync above)
            var articles = await _repo.GetForCategoriesAsync(categoryIds, count);
            return articles.Take(count).ToList();
        }

        public async Task<List<ArticleDto>> GetDeletedAsync()
        {
            var deleted = await _repo.GetDeletedAsync();

            return deleted.Select(a => new ArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Slug = a.Slug,
                Summary = a.Summary,
                Content = a.Content,
                FeaturedImageThumb = a.FeaturedImageThumb,
                FeaturedImageMedium = a.FeaturedImageMedium,
                FeaturedImageLarge = a.FeaturedImageLarge,
                AuthorId = a.AuthorId,
                ReporterId = a.ReporterId,
                CategoryId = a.CategoryId,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                WorkflowStatus = a.WorkflowStatus,
                ReviewerName = a.ReviewerUser?.FullName,
                FactCheckerName = a.FactCheckerUser?.FullName,
                WorkflowComment = a.WorkflowComment,
                IsEditorsPick = a.IsEditorsPick,
                EditorialPriority = a.EditorialPriority,
                IsFactChecked = a.IsFactChecked,
                AuthorName = a.Author?.User?.FullName,
                ReporterName = a.Reporter?.Name,
                ReporterSourceName = a.Reporter?.SourceName,
                CategoryName = a.Category?.Name,
                CategorySlug = a.Category?.Slug,
                IsDeleted = a.IsDeleted
            }).ToList();
        }
    }
}
