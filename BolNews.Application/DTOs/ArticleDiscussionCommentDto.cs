using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class ArticleDiscussionCommentDto
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
