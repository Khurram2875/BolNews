namespace BolNews.Infrastructure.Services.WordPressMigration
{
    /// <summary>
    /// Settings for importing from a WordPress site over its public REST API (no database access needed).
    /// Selected with "WordPressMigration:Source": "Rest" (default stays "Database").
    /// </summary>
    public sealed class WordPressRestOptions
    {
        public const string SectionName = "WordPressRest";

        /// <summary>Site root, e.g. https://urdu.bolnews.com (no trailing path).</summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>Posts per request (WordPress allows at most 100).</summary>
        public int PerPage { get; set; } = 50;

        /// <summary>Optional: WordPress user and application password, only needed for private/draft content.</summary>
        public string? Username { get; set; }
        public string? ApplicationPassword { get; set; }

        /// <summary>Safety cap on pages fetched per run.</summary>
        public int MaxPages { get; set; } = 400;
    }
}
