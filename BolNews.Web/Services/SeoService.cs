using System.Text.Json;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
namespace BolNews.Web.Services
{
    public class SeoService : ISeoService
    {
        private static string Slugify(string value)
        {
            return string.Join("-", value
                .Trim()
                .ToLowerInvariant()
                .Split(" ", StringSplitOptions.RemoveEmptyEntries));
        }
        public string BuildArticleSchema(PublicArticleVM article, string baseUrl)
        {
            var schema = new
            {
                @context = "https://schema.org",
                @type = "NewsArticle",

                headline = article.Title,
                description = article.MetaDescription,
                datePublished = article.PublishedAt,
                dateModified = article.UpdatedAt ?? article.PublishedAt,

                image = new[] { article.FeaturedImageXl },

                author = new
                {
                    @type = "Person",
                    name = article.AuthorName,
                    url = $"{baseUrl}/author/{article.AuthorSlug}"
                },

                publisher = new
                {
                    @type = "Organization",
                    name = "Bol News",
                    logo = new
                    {
                        @type = "ImageObject",
                        url = $"{baseUrl}/logo.png"
                    }
                },

                mainEntityOfPage = $"{baseUrl}/news/{article.CategorySlug}/{article.Slug}"
            };

            return JsonSerializer.Serialize(schema);
        }

        public string BuildCategorySchema(
            string categoryName,
            string categorySlug,
            string metaDescription,
            List<PublicArticleVM> articles,
            string baseUrl)
        {
            var schema = new
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

            return JsonSerializer.Serialize(schema);
        }

        public string BuildOrganizationSchema(string baseUrl)
        {
            var schema = new
            {
                @context = "https://schema.org",
                @type = "Organization",

                name = "Bol News",
                url = baseUrl,

                logo = new
                {
                    @type = "ImageObject",
                    url = $"{baseUrl}/logo.png"
                }
            };

            return JsonSerializer.Serialize(schema);
        }
        public string BuildBreadcrumb(PublicArticleVM article, string baseUrl)
        {
            var schema = new
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
                    item = $"{baseUrl}/news/{article.CategorySlug}"
                },
                new {
                        @type = "ListItem",
                        position = 3,
                        name = article.Title,
                        item = $"{baseUrl}/news/{article.CategorySlug}/{article.Slug}"
                    }
                }
            };
            return JsonSerializer.Serialize(schema);
        }
        public string BuildCategoryBreadcrumb(string categoryName, string categorySlug, string baseUrl)
        {
            var schema = new
            {
                @context = "https://schema.org",
                @type = "BreadcrumbList",

                itemListElement = new object[]
                {
            new
            {
                @type = "ListItem",
                position = 1,
                name = "Home",
                item = baseUrl
            },
            new
            {
                @type = "ListItem",
                position = 2,
                name = categoryName,
                item = $"{baseUrl}/news/{categorySlug}"
            }
                }
            };

            return JsonSerializer.Serialize(schema);
        }
        public string BuildAuthorSchema(AuthorPageVM author)
        {
            var schema = new
            {
                @context = "https://schema.org",
                @type = "Person",
                name = author.Name,
                description = author.Bio,
                image = author.ProfileImage,
                url = $"{author.BaseUrl}/author/{Slugify(author.Name)}",
                sameAs = new string[]
                {
                    // optional social links later
                }
            };

            return JsonSerializer.Serialize(schema);
        }
    }
}
