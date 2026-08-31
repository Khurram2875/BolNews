using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IWordPressAuthorResolver
    {
        Task<Author?> ResolveAsync(
            int? wordpressAuthorId,
            string? wordpressAuthorName,
            CancellationToken cancellationToken = default);
    }
}
