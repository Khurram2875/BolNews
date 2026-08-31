using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.WordPressMigration
{
    public class WordPressReporterResolver : IWordPressReporterResolver
    {
        private readonly IReporterRepository _reporterRepository;

        public WordPressReporterResolver(
            IReporterRepository reporterRepository)
        {
            _reporterRepository = reporterRepository;
        }

        public async Task<List<Reporter>> ResolveAsync(
            string? reporters,
            CancellationToken cancellationToken = default)
        {
            var result = new List<Reporter>();

            if (string.IsNullOrWhiteSpace(reporters))
                return result;

            var reporterEntries = reporters
                .Split(
                    "###",
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            foreach (var entry in reporterEntries)
            {
                var parts = entry.Split(
                    "|||",
                    StringSplitOptions.None);

                if (parts.Length < 3)
                    continue;

                var name = parts[1].Trim();
                var slug = parts[2].Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(slug))
                {
                    continue;
                }

                // Match existing CMS reporter by slug.
                var existingReporter =
                    await _reporterRepository.FindBySlugAsync(slug);

                if (existingReporter != null)
                {
                    result.Add(existingReporter);
                    continue;
                }

                // Reporter doesn't exist → create it.
                var reporter = new Reporter
                {
                    Name = name,
                    Slug = slug,
                    SourceName = null,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                await _reporterRepository.AddAsync(reporter);

                result.Add(reporter);
            }

            return result;
        }
    }
}
