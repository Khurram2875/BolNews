namespace BolNews.Web.Configuration
{
    public class LatestNewsSettings
    {
        /// <summary>
        /// If non-empty, ONLY articles from these category slugs are included
        /// in "Latest News". Leave empty to include every category.
        /// </summary>
        public static readonly string[] IncludedCategorySlugs = Array.Empty<string>();

        /// <summary>
        /// Category slugs to always exclude from "Latest News",
        /// even if IncludedCategorySlugs is empty (i.e. "include all").
        /// </summary>
        public static readonly string[] ExcludedCategorySlugs =
        {
            // e.g. "sponsored", "press-release"
        };

        public static int DefaultCount => 20;
    }
}
