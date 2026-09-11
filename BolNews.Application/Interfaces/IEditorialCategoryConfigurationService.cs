using BolNews.Application.DTOs.Editorial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IEditorialCategoryConfigurationService
    {
        Task<List<EditorialCategoryConfigurationDto>> GetByPlacementTypeAsync(
            string placementType);

        Task<Dictionary<string, List<EditorialCategoryConfigurationDto>>>
            GetAllAsync();

        Task SaveAsync(
            string placementType,
            List<int> categoryIds);
    }
}
