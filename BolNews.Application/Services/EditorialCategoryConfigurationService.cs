using BolNews.Application.DTOs.Editorial;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Services
{
    public class EditorialCategoryConfigurationService : IEditorialCategoryConfigurationService 
    { 
        private readonly IEditorialCategoryConfigurationRepository _repository; 
        private readonly ICacheService _cacheService; 
        private const string CacheKey = "editorial_category_configurations"; 
        public EditorialCategoryConfigurationService(IEditorialCategoryConfigurationRepository repository, ICacheService cacheService) 
        { 
            _repository = repository; 
            _cacheService = cacheService; 
        } 
        public async Task<List<EditorialCategoryConfigurationDto>> GetByPlacementTypeAsync(string placementType)
        { 
            var configurations = await _repository.GetByPlacementTypeAsync(placementType); 
            return configurations.Select(x => new EditorialCategoryConfigurationDto 
            { 
                Id = x.Id, PlacementType = x.PlacementType, CategoryId = x.CategoryId, 
                CategoryName = x.Category?.Name ?? string.Empty, SortOrder = x.SortOrder, 
                IsActive = x.IsActive 
            }).ToList(); 
        } 
        public async Task<Dictionary<string, List<EditorialCategoryConfigurationDto>>> GetAllAsync() 
        { 
            return await _cacheService.GetOrCreateAsync(CacheKey, async () => 
            { 
                var configurations = await _repository.GetAllAsync(); 
                return configurations.GroupBy(x => x.PlacementType).ToDictionary(group => group.Key, group => group.OrderBy(x => x.SortOrder)
                .Select(x => new EditorialCategoryConfigurationDto 
                { 
                    Id = x.Id, 
                    PlacementType = x.PlacementType, 
                    CategoryId = x.CategoryId, 
                    CategoryName = x.Category?.Name ?? string.Empty, 
                    SortOrder = x.SortOrder, IsActive = x.IsActive 
                }).ToList()); 
            }, minutes: 30); 
        } 
        public async Task SaveAsync(string placementType, List<int> categoryIds) 
        { 
            if (string.IsNullOrWhiteSpace(placementType)) 
            { 
                throw new ArgumentException("Placement type is required.", nameof(placementType)); 
            } 
            categoryIds ??= new List<int>(); var uniqueCategoryIds = categoryIds
                .Where(id => id > 0).Distinct().ToList(); 
            await _repository.DeleteByPlacementTypeAsync(placementType); 
            for (var i = 0; i < uniqueCategoryIds.Count; i++) 
            { 
                await _repository.AddAsync(new EditorialCategoryConfiguration 
                { 
                    PlacementType = placementType, 
                    CategoryId = uniqueCategoryIds[i], 
                    SortOrder = i, IsActive = true 
                }); 
            } 
            await _repository.SaveChangesAsync(); 
            _cacheService.Remove(CacheKey); 
        } 
        //public Task DeleteAsync(int id) 
        //{ 
        //    throw new NotSupportedException("Individual category configuration deletion is not supported. " + "Use SaveAsync to replace the complete category selection."); 
        //} 
    }
}
