using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using BolNews.Web.SEO;
using System.Text.Json;
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
            var keywords = BuildKeywords(article);
            var schema = new
            {
                @context = "https://schema.org",
                @type = "NewsArticle",

                headline = article.Title,
                description = article.MetaDescription,
                datePublished = article.PublishedAt,
                dateModified = article.UpdatedAt ?? article.PublishedAt,

                keywords,

                about = article.ArticleTags.Select(tag => new
                {
                    @type = "Thing",
                    name = tag.Name,
                    url = $"{baseUrl}/tag/{tag.Slug}"
                }),

                image = new[]
                {
                    new
                    {
                        @type = "ImageObject",
                        url = article.FeaturedImageXl,
                        caption = article.FeaturedImageCaption,
                        creditText = article.FeaturedImageCredit,
                        description = string.IsNullOrWhiteSpace(article.FeaturedImageAltText)
                            ? article.Title
                            : article.FeaturedImageAltText,
                        keywords = string.Join(", ", article.FeaturedImageTags.Select(tag => tag.Name))
                    }
                },

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

                mainEntityOfPage = $"{baseUrl}/{article.CategorySlug}/{article.Slug}"
            };

            return JsonSerializer.Serialize(schema);
        }

        public string BuildKeywords(PublicArticleVM article)
        {
            return string.Join(", ", article.ArticleTags
                .Select(tag => tag.Name)
                .Concat(new[] { article.CategoryName, article.AuthorName })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase));
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
                item = $"{baseUrl}/category/{categorySlug}"
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
                url = $"{author.BaseUrl}/author/{(string.IsNullOrWhiteSpace(author.Slug) ? Slugify(author.Name) : author.Slug)}",
                sameAs = new string[]
                {
                    // optional social links later
                }
            };

            return JsonSerializer.Serialize(schema);
        }

        public string BuildWebSiteSchema(string baseUrl)
        {
            return JsonSerializer.Serialize(
                StructuredDataBuilder.BuildWebSite(baseUrl));
        }
    }
}
