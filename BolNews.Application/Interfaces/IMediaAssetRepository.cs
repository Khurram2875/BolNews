using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Interfaces;

public interface IMediaAssetRepository
{
    Task<int> AddAsync(MediaAsset asset);
    Task<MediaAsset?> GetByIdAsync(int id);
    Task<List<MediaAsset>> SearchAsync(string? query, MediaType? type, DateTime? createdFrom, DateTime? createdTo, int? articleId);
    Task UpdateAsync(MediaAsset asset);
    Task AssignAsFeaturedAsync(int articleId, int mediaAssetId, string currentUserId);
}
