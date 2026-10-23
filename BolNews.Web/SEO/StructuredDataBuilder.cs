using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.SEO
{
    public static class StructuredDataBuilder
    {
        private static string ToAbsoluteUrl(string value, string baseUrl)
        {
            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute)
                && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
                return absolute.ToString();

            return $"{baseUrl.TrimEnd('/')}/{value.TrimStart('/')}";
        }

        public static object BuildNewsArticle(PublicArticleVM article, string baseUrl, string publisherName = "Publisher", string logoUrl = "/")
        {
            return new
            {
                @context = "https://schema.org",
                @type = "NewsArticle",

                mainEntityOfPage = new
                {
                    @type = "WebPage",
                    @id = $"{baseUrl}/{article.CategorySlug}/{article.Slug}"
                },

                headline = article.Title,
                description = article.MetaDescription,

                //image = article.Images.Select(i => i.Url),
                image = new[] { article.FeaturedImageXl, article.FeaturedImageLarge, article.FeaturedImageMedium, article.FeaturedImageThumb },

                datePublished = article.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                dateModified = article.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),

                author = new
                {
                    @type = "Person",
                    name = article.AuthorName,
                    url = $"{baseUrl}/author/{article.AuthorSlug}"
                },

                publisher = new
                {
                    @type = "Organization",
                    name = publisherName,
                    url = baseUrl,

                    logo = new
                    {
                        @type = "ImageObject",
                        url = ToAbsoluteUrl(logoUrl, baseUrl)
                    }
                }
            };
        }

        public static object BuildBreadcrumb(PublicArticleVM article, string baseUrl)
        {
            return new
            {
                @context = "https://schema.org",
                @type = "BreadcrumbList",
                itemListElement = new object[]
                {
                new {
                    @type = "ListItem",
                    position = 1,
                    name = "Home",
                    item = baseUrl
                },
                new {
                    @type = "ListItem",
                    position = 2,
                    name = article.CategoryName,
                        item = $"{baseUrl}/category/{article.CategorySlug}"
                },
                new {
                        @type = "ListItem",
                        position = 3,
                        name = article.Title,
                        item = $"{baseUrl}/{article.CategorySlug}/{article.Slug}"
                    }
                }
            };
        }
        public static object BuildOrganization(string baseUrl, string publisherName = "Publisher", string logoUrl = "/", IEnumerable<string>? socialLinks = null)
        {
            return new
            {
                @context = "https://schema.org",
                @type = "Organization",

                name = publisherName,
                url = baseUrl,

                logo = new
                {
                    @type = "ImageObject",
                    url = ToAbsoluteUrl(logoUrl, baseUrl)
                },

                sameAs = (socialLinks ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            };
        }
        public static object BuildCategoryPage(
            string categoryName,
            string categorySlug,
            string metaDescription,
            List<PublicArticleVM> articles,
            string baseUrl)
        {
            return new
            {
                @context = "https://schema.org",
                @type = "CollectionPage",

                name = categoryName,
                description = metaDescription,
                url = $"{baseUrl}/category/{categorySlug}",

                mainEntity = new
                {
                    @type = "ItemList",
                    itemListElement = articles.Select((a, index) => new
                    {
                        @type = "ListItem",
                        position = index + 1,
                        url = $"{baseUrl}/{a.CategorySlug}/{a.Slug}"
                    })
                }
            };
        }
        public static object BuildWebSite(string baseUrl, string publisherName = "Publisher")
        {
            return new
            {
                @context = "https://schema.org",
                @type = "WebSite",

                name = publisherName,

                url = baseUrl,

                potentialAction = new
                {
                    @type = "SearchAction",

                    target =
                        $"{baseUrl}/search?q={{search_term_string}}",

                    query_input =
                        "required name=search_term_string"
                }
            };
        }
    }
}
