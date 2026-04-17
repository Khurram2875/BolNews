using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class Article : BaseEntity
    {
        public string Title { get; set; }
        // ✅ SEO Fields
        public string Slug { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }

        public string Summary { get; set; }
        public string Content { get; set; }

        //public string? FeaturedImageUrl { get; set; }
        // Featured Images (Multi Size)
        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageXl { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public int AuthorId { get; set; }
        public Author Author { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        public int ViewCount { get; set; }
    }
}
