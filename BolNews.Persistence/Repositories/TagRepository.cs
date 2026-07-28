using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Persistence.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly AppDbContext _context;

        public TagRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tag>> GetAllAsync()
            => await _context.Tags
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<List<Tag>> GetByNormalizedNamesAsync(IReadOnlyCollection<string> normalizedNames)
            => await _context.Tags
                .Where(x => normalizedNames.Contains(x.NormalizedName))
                .ToListAsync();

        public async Task<Tag?> GetBySlugAsync(string slug)
            => await _context.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == slug);

        public async Task<List<Tag>> GetArticleTagsAsync(int articleId)
            => await _context.ArticleTags
                .AsNoTracking()
                .Where(x => x.ArticleId == articleId)
                .Select(x => x.Tag)
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<List<Tag>> GetFeaturedImageTagsAsync(int articleId)
            => await _context.FeaturedImageTags
                .AsNoTracking()
                .Where(x => x.FeaturedImageMetadata.ArticleId == articleId)
                .Select(x => x.Tag)
                .OrderBy(x => x.Name)
                .ToListAsync();

        public async Task<FeaturedImageMetadata?> GetFeaturedImageMetadataAsync(int articleId)
            => await _context.FeaturedImageMetadata
                .Include(x => x.FeaturedImageTags)
                    .ThenInclude(x => x.Tag)
                .FirstOrDefaultAsync(x => x.ArticleId == articleId);

        public Task AddTagsAsync(IEnumerable<Tag> tags)
        {
            _context.Tags.AddRange(tags);
            return Task.CompletedTask;
        }

        public async Task ReplaceArticleTagsAsync(int articleId, IReadOnlyCollection<Tag> tags, string currentUserId)
        {
            var existing = await _context.ArticleTags
                .Where(x => x.ArticleId == articleId)
                .ToListAsync();

            _context.ArticleTags.RemoveRange(existing);

            foreach (var tag in tags)
            {
                _context.ArticleTags.Add(new ArticleTag
                {
                    ArticleId = articleId,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                });
            }
        }

        public async Task ReplaceFeaturedImageTagsAsync(
            int articleId,
            IReadOnlyCollection<Tag> tags,
            string? altText,
            string? caption,
            string? credit,
            string currentUserId)
        {
            var metadata = await _context.FeaturedImageMetadata
                .Include(x => x.FeaturedImageTags)
                .FirstOrDefaultAsync(x => x.ArticleId == articleId);

            if (metadata == null)
            {
                metadata = new FeaturedImageMetadata
                {
                    ArticleId = articleId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };
                _context.FeaturedImageMetadata.Add(metadata);
                await _context.SaveChangesAsync();
            }

            metadata.AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
            metadata.Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
            metadata.Credit = string.IsNullOrWhiteSpace(credit) ? null : credit.Trim();
            metadata.UpdatedAt = DateTime.UtcNow;
            metadata.UpdatedBy = currentUserId;

            _context.FeaturedImageTags.RemoveRange(metadata.FeaturedImageTags);

            foreach (var tag in tags)
            {
                _context.FeaturedImageTags.Add(new FeaturedImageTag
                {
                    FeaturedImageMetadataId = metadata.Id,
                    TagId = tag.Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                });
            }
        }

        public async Task ReplaceMediaAssetTagsAsync(int mediaAssetId, IReadOnlyCollection<Tag> tags, string currentUserId)
        {
            var existing = await _context.MediaAssetTags.Where(x => x.MediaAssetId == mediaAssetId).ToListAsync();
            _context.MediaAssetTags.RemoveRange(existing);
            foreach (var tag in tags) _context.MediaAssetTags.Add(new MediaAssetTag { MediaAssetId = mediaAssetId, TagId = tag.Id });
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
