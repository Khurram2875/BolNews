using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IAdPlacementRepository
    {
        Task<AdPlacement?> GetByIdAsync(int id);

        Task<AdPlacement?> GetByPlacementKeyAsync(string placementKey);

        Task<List<AdPlacement>> GetAllAsync();

        Task AddAsync(AdPlacement entity);

        Task UpdateAsync(AdPlacement entity);

        Task DeleteAsync(AdPlacement entity);

        Task SaveAsync();
        Task ToggleStatusAsync(int id, bool isEnabled);
    }
}
