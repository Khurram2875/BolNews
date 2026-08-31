using BolNews.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IWordPressCategoryResolver
    {
        Task<Category?> ResolveAsync(
            int? wordpressCategoryId,
            string? categoryName,
            string? categorySlug,
            CancellationToken cancellationToken = default);
    }
}
