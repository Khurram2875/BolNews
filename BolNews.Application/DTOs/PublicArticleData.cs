using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public sealed class PublicArticleData
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public string? Summary { get; set; }
        public string? Content { get; set; }

        public string? FeaturedImageThumb { get; set; }
        public string? FeaturedImageMedium { get; set; }
        public string? FeaturedImageLarge { get; set; }
        public string? FeaturedImageXl { get; set; }

        public string? FeaturedImageAltText { get; set; }
        public string? FeaturedImageCaption { get; set; }
        public string? FeaturedImageCredit { get; set; }

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategorySlug { get; set; } = string.Empty;

        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorSlug { get; set; } = string.Empty;
        public string AuthorImage { get; set; } = string.Empty;

        public int? ReporterId { get; set; }
        public string? ReporterName { get; set; }
        public string? ReporterSourceName { get; set; }

        public List<TagDto> ArticleTags { get; set; } = new();
        public List<TagDto> FeaturedImageTags { get; set; } = new();

        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<int> ArticleTagIds { get; set; } = new();
    }
}
