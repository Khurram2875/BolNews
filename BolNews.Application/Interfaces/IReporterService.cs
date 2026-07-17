using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface IReporterService
    {
        Task<List<ReporterDto>> GetAllAsync();

        Task<ReporterDto?> GetByIdAsync(int id);

        Task<ReporterDto> CreateAsync(ReporterDto dto, string currentUserId);

        Task<ReporterDto> UpdateAsync(ReporterDto dto, string currentUserId);

        Task DeleteAsync(int id, string currentUserId);
    }
}
