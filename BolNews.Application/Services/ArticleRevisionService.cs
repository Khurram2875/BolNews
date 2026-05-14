using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
