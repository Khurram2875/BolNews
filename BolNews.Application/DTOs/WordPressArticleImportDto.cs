using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class WordPressArticleImportDto
    {
        public int WordPressPostId { get; set; }

        public DateTime PostDate { get; set; }
        public DateTime PostModifiedDate { get; set; }

        public string PostTitle { get; set; } = string.Empty;
        public string? PostSummary { get; set; }
        public string? PostContent { get; set; }
        public string Slug { get; set; } = string.Empty;

        public string PostStatus { get; set; } = string.Empty;
        public string PostType { get; set; } = string.Empty;

        // WordPress author
        public int? WordPressAuthorId { get; set; }
        public string? AuthorName { get; set; }

        // WordPress category
        public int? WordPressCategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? CategorySlug { get; set; }

        // WordPress reporter
        // Format:
        // ID|||Name|||Slug###ID|||Name|||Slug
        public string? Reporters { get; set; }

        // Featured image
        public int? FeaturedImageId { get; set; }
        public string? FeaturedImagePath { get; set; }
        public string? FeaturedImageTitle { get; set; }

        // Tags
        // Format:
        // Name|||Slug###Name|||Slug
        public string? PostTags { get; set; }

        // Featured image tags
        public string? FeaturedImageTags { get; set; }

        public int? WordPressPrimaryReporterId { get; set; }

        public string? PrimaryReporterName { get; set; }

        public string? PrimaryReporterSlug { get; set; }
    }
}
