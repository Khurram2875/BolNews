using BolNews.Application.DTOs;
using BolNews.Application.Common.Helpers;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class ReporterService : IReporterService
    {
        private readonly IReporterRepository _repository;

        public ReporterService(IReporterRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ReporterDto>> GetAllAsync()
        {
            var reporters =
                await _repository.GetAllAsync();

            return reporters
                .Select(ToDto)
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<ReporterDto?> GetByIdAsync(int id)
        {
            var reporter =
                await _repository.FindByIdAsync(id);

            return reporter == null
                ? null
                : ToDto(reporter);
        }

        public async Task<ReporterDto> CreateAsync(ReporterDto dto, string currentUserId)
        {
            var reporter = new Reporter
            {
                Name = dto.Name.Trim(),
                SourceName = string.IsNullOrWhiteSpace(dto.SourceName)
                    ? null
                    : dto.SourceName.Trim(),
                Slug = BuildSlug(dto),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId
            };

            await _repository.AddAsync(reporter);

            dto.Id = reporter.Id;
            dto.Slug = reporter.Slug;
            return dto;
        }

        public async Task<ReporterDto> UpdateAsync(ReporterDto dto, string currentUserId)
        {
            var reporter = await _repository.FindByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException("Reporter not found");

            reporter.Name = dto.Name.Trim();
            reporter.SourceName = string.IsNullOrWhiteSpace(dto.SourceName)
                ? null
                : dto.SourceName.Trim();
            reporter.Slug = BuildSlug(dto);
            reporter.UpdatedAt = DateTime.UtcNow;
            reporter.UpdatedBy = currentUserId;

            await _repository.UpdateAsync(reporter);

            dto.Slug = reporter.Slug;
            return dto;
        }

        public async Task DeleteAsync(int id, string currentUserId)
        {
            var reporter = await _repository.FindByIdAsync(id)
                ?? throw new KeyNotFoundException("Reporter not found");

            reporter.IsDeleted = true;
            reporter.UpdatedAt = DateTime.UtcNow;
            reporter.UpdatedBy = currentUserId;

            await _repository.UpdateAsync(reporter);
        }

        private static ReporterDto ToDto(Reporter reporter)
        {
            return new ReporterDto
            {
                Id = reporter.Id,
                Name = reporter.Name,
                SourceName = reporter.SourceName,
                Slug = reporter.Slug
            };
        }

        private static string BuildSlug(ReporterDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Slug))
                return SlugHelper.GenerateSlug(dto.Slug);

            return SlugHelper.GenerateSlug(dto.Name);
        }
    }
}
