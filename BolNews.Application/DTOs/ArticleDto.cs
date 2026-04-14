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

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        public string? FeaturedImageUrl { get; set; }
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }

        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; } // 🔥 ADD THIS

        public int AuthorId { get; set; }
        public string? AuthorName { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
