using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Persistence.Repositories
{
    public class EditorialCategoryConfigurationRepository
        : IEditorialCategoryConfigurationRepository
    {
        private readonly AppDbContext _context;

        public EditorialCategoryConfigurationRepository(
            AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<EditorialCategoryConfiguration>>
            GetByPlacementTypeAsync(string placementType)
        {
            return await _context.EditorialCategoryConfigurations
                .AsNoTracking()
                .Include(x => x.Category)
                .Where(x =>
                    x.PlacementType == placementType &&
                    x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
        }

        public async Task<List<EditorialCategoryConfiguration>>
            GetAllAsync()
        {
            return await _context.EditorialCategoryConfigurations
                .AsNoTracking()
                .Include(x => x.Category)
                .OrderBy(x => x.PlacementType)
                .ThenBy(x => x.SortOrder)
                .ToListAsync();
        }

        public async Task AddAsync(
            EditorialCategoryConfiguration configuration)
        {
            await _context.EditorialCategoryConfigurations
                .AddAsync(configuration);
        }

        public async Task DeleteByPlacementTypeAsync(
            string placementType)
        {
            await _context.EditorialCategoryConfigurations
                .Where(x => x.PlacementType == placementType)
                .ExecuteDeleteAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
