using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IArticleRevisionService
    {
        Task CreateSnapshotAsync(
            Article article,
            string changedByUserId,
            string workflowState,
            string? changeReason = null);

        Task<List<ArticleRevisionDto>> GetByArticleIdAsync(int articleId);

        Task<ArticleRevisionDto?> GetByIdAsync(int revisionId);
    }
}
