using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.DTOs
{
    public class AuthorDto
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>URL-friendly slug derived from the author's name.</summary>
        public string? Slug { get; set; }

        public string Bio { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }

        /// <summary>Populated only when articles are explicitly included.</summary>
        public ICollection<Article>? Articles { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}
