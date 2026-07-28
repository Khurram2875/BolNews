using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface ITagService
    {
        string NormalizeName(string value);
        IReadOnlyList<string> ParseTagInput(string? input);
        Task<List<TagDto>> GetAllAsync();
        Task<TagDto?> GetBySlugAsync(string slug);
        Task<List<TagDto>> GetArticleTagsAsync(int articleId);
        Task<List<TagDto>> GetFeaturedImageTagsAsync(int articleId);
        Task ReplaceArticleTagsAsync(int articleId, string? tagInput, string currentUserId);
        Task ReplaceFeaturedImageTagsAsync(
            int articleId,
            string? tagInput,
            string? altText,
            string? caption,
            string? credit,
            string currentUserId);
        Task ReplaceMediaAssetTagsAsync(int mediaAssetId, string? tagInput, string currentUserId);
    }
}
