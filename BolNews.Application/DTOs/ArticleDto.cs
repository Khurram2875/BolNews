using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class ArticleDto
    {
        public int Id { get; set; }

        public string Title { get; set; }
        public string Slug { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string? FeaturedImageUrl { get; set; }

        public int CategoryId { get; set; }
        public int AuthorId { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
    }
}
