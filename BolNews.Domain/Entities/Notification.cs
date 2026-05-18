using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities.Base;

namespace BolNews.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string UserId { get; set; }

        public ApplicationUser User { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; }

        public string? Url { get; set; }
    }
}
