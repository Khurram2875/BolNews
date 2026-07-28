using BolNews.Application.DTOs;
using BolNews.Domain.Enums;

namespace BolNews.Application.Interfaces;

public interface IMediaLibraryService
{
    Task<List<MediaAssetDto>> SearchAsync(string? query, MediaType? type, DateTime? createdFrom, DateTime? createdTo, int? articleId);
    Task<MediaAssetDto?> GetByIdAsync(int id);
    Task<int> CreateAsync(MediaAssetDto dto, string currentUserId);
    Task UpdateAsync(MediaAssetDto dto, string currentUserId);
    Task SetStorageAsync(int id, string url, string? thumb, string? medium, string? large, string currentUserId);
    Task AssignAsFeaturedAsync(int articleId, int mediaAssetId, string currentUserId);
}
