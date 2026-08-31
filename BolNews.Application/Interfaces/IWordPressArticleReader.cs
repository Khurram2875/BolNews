using BolNews.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IWordPressArticleReader
    {
        Task<List<WordPressArticleImportDto>> GetArticlesAsync(
            DateTime fromDate,
            DateTime toDate,
            CancellationToken cancellationToken = default);
    }
}
