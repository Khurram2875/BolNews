using BolNews.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IWordPressArticleImportService
    {
        Task<WordPressImportResultDto> ImportAsync(
            DateTime fromDate,
            DateTime toDate,
            string currentUserId,
            int? take = null,
            CancellationToken cancellationToken = default);
    }
}
