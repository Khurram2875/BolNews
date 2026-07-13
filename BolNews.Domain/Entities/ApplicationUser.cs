using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace BolNews.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfileImage { get; set; }
        //For linking of breaking news Ticker
        public virtual ICollection<BreakingNews> CreatedBreakingNews { get; set; } = new List<BreakingNews>();

        public virtual ICollection<BreakingNews> UpdatedBreakingNews { get; set; } = new List<BreakingNews>();
    }
}
