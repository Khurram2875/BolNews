using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.DTOs
{
    public class AuthorDto2
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string slug { get; set; } 
        public string Bio { get; set; } = string.Empty;

        public string? ProfileImageUrl { get; set; }
        public ICollection<Article> Articles { get; set; }
        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
    }
}
