using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class TrendingTopicDto
    {
        public string Title { get; set; }
        public string Traffic { get; set; }
        public string Source { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
    }
}
