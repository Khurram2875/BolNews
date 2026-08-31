using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IWordPressReporterResolver
    {
        Task<List<Reporter>> ResolveAsync(
            string? reporters,
            CancellationToken cancellationToken = default);
    }
}
