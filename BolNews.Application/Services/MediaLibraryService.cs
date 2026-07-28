using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services;

public class MediaLibraryService(IMediaAssetRepository repository, ITagService tagService) : IMediaLibraryService
{
    public async Task<List<MediaAssetDto>> SearchAsync(string? query, MediaType? type, DateTime? createdFrom, DateTime? createdTo, int? articleId) => (await repository.SearchAsync(query, type, createdFrom, createdTo, articleId)).Select(Map).ToList();
    public async Task<MediaAssetDto?> GetByIdAsync(int id) { var item = await repository.GetByIdAsync(id); return item == null ? null : Map(item); }
    public async Task<int> CreateAsync(MediaAssetDto dto, string currentUserId)
    {
        var id = await repository.AddAsync(new MediaAsset { MediaType = dto.MediaType, Url = dto.Url, AltText = dto.AltText?.Trim(), Caption = dto.Caption?.Trim(), Credit = dto.Credit?.Trim(), OriginalFileName = dto.OriginalFileName, CreatedAt = DateTime.UtcNow, CreatedBy = currentUserId });
        await tagService.ReplaceMediaAssetTagsAsync(id, dto.TagsInput, currentUserId); return id;
    }
    public async Task UpdateAsync(MediaAssetDto dto, string currentUserId)
    {
        var item = await repository.GetByIdAsync(dto.Id) ?? throw new KeyNotFoundException("Media was not found.");
        item.AltText = dto.AltText?.Trim(); item.Caption = dto.Caption?.Trim(); item.Credit = dto.Credit?.Trim(); item.UpdatedAt = DateTime.UtcNow; item.UpdatedBy = currentUserId;
        await repository.UpdateAsync(item); await tagService.ReplaceMediaAssetTagsAsync(item.Id, dto.TagsInput, currentUserId);
    }
    public async Task SetStorageAsync(int id, string url, string? thumb, string? medium, string? large, string currentUserId)
    { var item = await repository.GetByIdAsync(id) ?? throw new KeyNotFoundException("Media was not found."); item.Url = url; item.ThumbnailUrl = thumb; item.MediumUrl = medium; item.LargeUrl = large; item.UpdatedAt = DateTime.UtcNow; item.UpdatedBy = currentUserId; await repository.UpdateAsync(item); }
    public Task AssignAsFeaturedAsync(int articleId, int mediaAssetId, string currentUserId) => repository.AssignAsFeaturedAsync(articleId, mediaAssetId, currentUserId);
    private static MediaAssetDto Map(MediaAsset x) => new() { Id=x.Id, MediaType=x.MediaType, Url=x.Url, ThumbnailUrl=x.ThumbnailUrl, MediumUrl=x.MediumUrl, LargeUrl=x.LargeUrl, AltText=x.AltText, Caption=x.Caption, Credit=x.Credit, OriginalFileName=x.OriginalFileName, Tags=x.MediaAssetTags.Select(t => new TagDto { Id=t.TagId, Name=t.Tag.Name, Slug=t.Tag.Slug }).OrderBy(t=>t.Name).ToList(), TagsInput=System.Text.Json.JsonSerializer.Serialize(x.MediaAssetTags.Select(t=>t.Tag.Name)), UsedByArticles=x.FeaturedForArticles.Select(a=>a.Title).ToList(), CreatedAt=x.CreatedAt };
}
