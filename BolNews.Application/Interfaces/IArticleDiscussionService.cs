using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface IArticleDiscussionService
    {
        Task<List<ArticleDiscussionCommentDto>> GetThreadAsync(int articleId, string currentUserId, IList<string> roles);

        Task AddCommentAsync(int articleId, string message, string currentUserId, IList<string> roles);
       
    }
}
