using BolNews.Domain.Entities.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Entities
{
    public class EditorialCategoryConfiguration : BaseEntity
    {
        public string PlacementType { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
