using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Entities
{
    public class ArticleDiscussionComment
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation
        public Article Article { get; set; } = null!;

        public ApplicationUser User { get; set; } = null!;
    }
}
