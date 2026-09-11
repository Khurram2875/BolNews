using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IEditorialCategoryConfigurationRepository
    {
        Task<List<EditorialCategoryConfiguration>> GetByPlacementTypeAsync(
            string placementType);

        Task<List<EditorialCategoryConfiguration>> GetAllAsync();

        Task AddAsync(
            EditorialCategoryConfiguration configuration);

        Task DeleteByPlacementTypeAsync(
            string placementType);

        Task SaveChangesAsync();
    }
}
