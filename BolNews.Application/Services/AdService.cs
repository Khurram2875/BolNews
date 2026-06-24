using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using BolNews.Domain.Entities;
using BolNews.Domains.ViewModels;
using Microsoft.Extensions.Options;

namespace BolNews.Application.Services
{
    public class AdService : IAdService
    {
        private readonly AdOptions _options;
        private readonly IAdPlacementRepository _adPlacementRepo;

        public AdService(IOptions<AdOptions> options, IAdPlacementRepository adPlacementRepo)
        {
            _options = options.Value;
            _adPlacementRepo = adPlacementRepo;
        }

        public string GetHeaderAd()
        {
            return _options.Enabled ? _options.HeaderAd : string.Empty;
        }

        public string GetSidebarAd()
        {
            return _options.Enabled ? _options.SidebarAd : string.Empty;
        }

        public string GetInArticleAd()
        {
            return _options.Enabled ? _options.InArticleAd : string.Empty;
        }

        public async Task<AdPlacementViewModel?> GetPlacementAsync(string placementKey)
        {
            var ad = await _adPlacementRepo.GetByPlacementKeyAsync(placementKey);

            if (ad == null)
                return null;

            return new AdPlacementViewModel
            {
              
                PlacementKey = ad.PlacementKey,
               
                AdCode = ad.AdCode,
                IsEnabled = ad.IsEnabled,
                
            };
        }

        public async Task<AdPlacement?> GetByIdAsync(int id)
        {
            return await _adPlacementRepo.GetByIdAsync(id);
        }

        public async Task<AdPlacement?> GetByPlacementKeyAsync(string placementKey)
        {
            return await _adPlacementRepo.GetByPlacementKeyAsync(placementKey);
        }

        public async Task<List<AdPlacement>> GetAllAsync()
        {
            return await _adPlacementRepo.GetAllAsync();
        }

        public async Task AddAsync(AdPlacement entity)
        {
             await _adPlacementRepo.AddAsync(entity);
        }

        public async Task UpdateAsync(AdPlacement entity)
        {
             await _adPlacementRepo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(AdPlacement entity)
        {
            await _adPlacementRepo.DeleteAsync(entity);
        }

        public async Task SaveAsync()
        {
            await _adPlacementRepo.SaveAsync();
        }
        public async Task ToggleStatusAsync(int id, bool isEnabled)
        {
            await _adPlacementRepo.ToggleStatusAsync(id, isEnabled);
            await _adPlacementRepo.SaveAsync();
        }

    }
}
