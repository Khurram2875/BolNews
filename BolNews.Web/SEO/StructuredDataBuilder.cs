using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.SEO
{
    public static class StructuredDataBuilder
    {

        public static object BuildNewsArticle(PublicArticleVM article, string baseUrl)
        {
            return new
            {
                @context = "https://schema.org",
                @type = "NewsArticle",

                mainEntityOfPage = new
                {
                    @type = "WebPage",
                    @id = $"{baseUrl}/news/{article.CategorySlug}/{article.Slug}"
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
                    name = article.AuthorName
                },

                publisher = new
                {
                    @type = "Organization",
                    name = "Bol News",
                    logo = new
                    {
                        @type = "ImageObject",
                        url = $"{baseUrl}/logo-black.png"
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
                        item = $"{baseUrl}/news/{article.CategorySlug}/{article.Slug}"
                    }
                }
            };
        }
        public static object BuildOrganization(string baseUrl)
        {
            return new
            {
                @context = "https://schema.org",
                @type = "Organization",

                name = "Bol News",
                url = baseUrl,

                logo = new
                {
                    @type = "ImageObject",
                    url = $"{baseUrl}/logo.png"
                },

                sameAs = new[]
                {
                    "https://www.facebook.com/bolnews",
                    "https://twitter.com/bolnews",
                    "https://www.youtube.com/bolnews"
                }
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
                url = $"{baseUrl}/news/{categorySlug}",

                mainEntity = new
                {
                    @type = "ItemList",
                    itemListElement = articles.Select((a, index) => new
                    {
                        @type = "ListItem",
                        position = index + 1,
                        url = $"{baseUrl}/news/{a.CategorySlug}/{a.Slug}"
                    })
                }
            };
        }
    }
}
