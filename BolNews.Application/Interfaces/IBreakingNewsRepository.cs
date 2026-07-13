using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    
        public interface IBreakingNewsRepository
        {
            Task<List<BreakingNews>> GetAllAsync();

            Task<List<BreakingNews>> GetActiveAsync();

            Task<List<BreakingNews>> GetInactiveAsync();

            Task<BreakingNews?> GetByIdAsync(int id);

            Task AddAsync(BreakingNews entity);

            Task UpdateAsync(BreakingNews entity);

            Task SaveChangesAsync();
        }
    
}
