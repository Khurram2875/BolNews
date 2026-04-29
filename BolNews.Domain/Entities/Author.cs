using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class Author : BaseEntity
    {
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string Bio { get; set; }

        public string? ProfileImageUrl { get; set; }

        public ICollection<Article>? Articles { get; set; }
    }
}
