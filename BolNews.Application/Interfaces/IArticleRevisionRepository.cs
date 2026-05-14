using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IArticleRevisionRepository
    {
        Task AddAsync(ArticleRevision revision);

        Task<int> GetNextRevisionNumberAsync(int articleId);

        Task<List<ArticleRevision>> GetByArticleIdAsync(int articleId);
    }
}
