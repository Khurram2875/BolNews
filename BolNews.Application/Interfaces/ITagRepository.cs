using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface ITagRepository
    {
        Task<List<Tag>> GetAllAsync();
        Task<List<Tag>> GetByNormalizedNamesAsync(IReadOnlyCollection<string> normalizedNames);
        Task<Tag?> GetBySlugAsync(string slug);
        Task<List<Tag>> GetArticleTagsAsync(int articleId);
        Task<List<Tag>> GetFeaturedImageTagsAsync(int articleId);
        Task<FeaturedImageMetadata?> GetFeaturedImageMetadataAsync(int articleId);
        Task AddTagsAsync(IEnumerable<Tag> tags);
        Task ReplaceArticleTagsAsync(int articleId, IReadOnlyCollection<Tag> tags, string currentUserId);
        Task ReplaceFeaturedImageTagsAsync(
            int articleId,
            IReadOnlyCollection<Tag> tags,
            string? altText,
            string? caption,
            string? credit,
            string currentUserId);
        Task SaveChangesAsync();
    }
}
