using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class ArticleRevisionService : IArticleRevisionService
    {
        private readonly IArticleRevisionRepository _repository;

        public ArticleRevisionService(
            IArticleRevisionRepository repository)
        {
            _repository = repository;
        }

        public async Task CreateSnapshotAsync(
            Article article,
            string changedByUserId,
            string workflowState,
            string? changeReason = null)
        {
            var revisionNumber =
                await _repository.GetNextRevisionNumberAsync(article.Id);

            var revision = new ArticleRevision
            {
                ArticleId = article.Id,
                RevisionNumber = revisionNumber,

                Title = article.Title,
                Slug = article.Slug,
                MetaTitle = article.MetaTitle,
                MetaDescription = article.MetaDescription,

                Summary = article.Summary,
                Content = article.Content,

                FeaturedImageThumb = article.FeaturedImageThumb,
                FeaturedImageMedium = article.FeaturedImageMedium,
                FeaturedImageLarge = article.FeaturedImageLarge,
                FeaturedImageXl = article.FeaturedImageXl,

                IsPublishedSnapshot = article.IsPublished,

                ChangedByUserId = changedByUserId,
                WorkflowState = workflowState,
                ChangeReason = changeReason,

                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(revision);
        }

        public async Task<List<ArticleRevisionDto>> GetByArticleIdAsync(
            int articleId)
        {
            var revisions =
                await _repository.GetByArticleIdAsync(articleId);

            return revisions
                .Select(Map)
                .ToList();
        }

        public async Task<ArticleRevisionDto?> GetByIdAsync(
            int revisionId)
        {
            var revision =
                await _repository.GetByIdAsync(revisionId);

            return revision == null
                ? null
                : Map(revision);
        }

        private static ArticleRevisionDto Map(
            ArticleRevision revision)
        {
            return new ArticleRevisionDto
            {
                Id = revision.Id,
                ArticleId = revision.ArticleId,
                RevisionNumber = revision.RevisionNumber,
                Title = revision.Title,
                Slug = revision.Slug,
                MetaTitle = revision.MetaTitle,
                MetaDescription = revision.MetaDescription,
                Summary = revision.Summary,
                Content = revision.Content,
                FeaturedImageThumb = revision.FeaturedImageThumb,
                FeaturedImageMedium = revision.FeaturedImageMedium,
                FeaturedImageLarge = revision.FeaturedImageLarge,
                FeaturedImageXl = revision.FeaturedImageXl,
                IsPublishedSnapshot = revision.IsPublishedSnapshot,
                ChangedByUserId = revision.ChangedByUserId,
                WorkflowState = revision.WorkflowState,
                ChangeReason = revision.ChangeReason,
                CreatedAt = revision.CreatedAt
            };
        }
    }
}
