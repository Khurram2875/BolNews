using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IReporterRepository
    {
        Task<List<Reporter>> GetAllAsync();

        Task<Reporter?> FindByIdAsync(int id);

        Task AddAsync(Reporter reporter);

        Task UpdateAsync(Reporter reporter);
    }
}
