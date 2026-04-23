using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Common
{
    public static class CacheKeys
    {
        public static string Article(string slug) => $"article_{slug}";

        public static string Category(string slug, int page)
            => $"category_{slug}_{page}";

        public static string Trending => "trending";

        public static string Dashboard => "dashboard";

        public static string Sitemap => "sitemap";
    }
}
