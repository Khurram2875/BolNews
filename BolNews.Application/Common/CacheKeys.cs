using System;
using System.Security.Cryptography;
using System.Text;

namespace BolNews.Application.Common
{
    public static class CacheKeys
    {
        public static string Article(string slug) => ArticlePage(slug);

        public static string ArticlePage(string slug)
            => $"article_page_{NormalizeKeyPart(slug)}";

        public static string Category(string slug, int page)
            => $"category_{NormalizeKeyPart(slug)}_{page}";

        public static string Search(string query, int page)
            => $"search_{Hash(NormalizeSearchQuery(query))}_{page}";

        public static string Trending(string type)
            => $"trending_{NormalizeKeyPart(type)}";

        public static string Dashboard => "dashboard";

        public static string Sitemap => "sitemap";

        public static string LatestNews(int count)
            => $"latest_news_{count}";

        public static string BreakingNews
            => "breaking_news";

        public static string NavbarCategories
            => "navbar_categories";

        public static string HomePage
            => "homepage";

        public static string HomePageIndex
            => $"{HomePage}_Index";

        public static string HomePageIndex1
            => $"{HomePage}_Index1";

        public static string LatestNewsFiltered(int count)
            => $"latest_news_filtered_{count}";

        private static string NormalizeKeyPart(string value)
            => (value ?? string.Empty).Trim().ToLowerInvariant();

        private static string NormalizeSearchQuery(string value)
            => (value ?? string.Empty).Trim().ToLowerInvariant();

        private static string Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
