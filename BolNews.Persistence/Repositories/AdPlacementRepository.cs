using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace BolNews.Persistence.Repositories
{
    public class AdPlacementRepository : IAdPlacementRepository
    {
        private readonly AppDbContext _context;
        private readonly DbSet<AdPlacement> _dbSet;
        public AdPlacementRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<AdPlacement>();
        }


        public async Task<AdPlacement?> GetByIdAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        // READ - Get by Placement Key (used in ViewComponent)
        public async Task<AdPlacement?> GetByPlacementKeyAsync(string placementKey)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.PlacementKey == placementKey &&
                    x.IsEnabled);
        }

        // READ - Get all (Admin listing)
        public async Task<List<AdPlacement>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        // CREATE
        public async Task AddAsync(AdPlacement entity)
        {
            await _dbSet.AddAsync(entity);
        }

        // UPDATE
        public Task UpdateAsync(AdPlacement entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        // DELETE
        public Task DeleteAsync(AdPlacement entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        // SAVE
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task ToggleStatusAsync(int id, bool isEnabled)
        {
            var ad = await _dbSet.FirstOrDefaultAsync(x => x.Id == id);

            if (ad == null)
                return;

            ad.IsEnabled = isEnabled;

            _dbSet.Update(ad);
        }
    }
}
