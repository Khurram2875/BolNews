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

        private static string? ToAbsoluteUrl(string? value, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute)
                && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                return absolute.ToString();
            }

            var path = value.StartsWith("/") ? value : $"/{value}";
            return $"{baseUrl.TrimEnd('/')}{path}";
        }

        public string BuildArticleSchema(PublicArticleVM article, string baseUrl)
        {
            var keywords = BuildKeywords(article);
            var articleUrl = $"{baseUrl.TrimEnd('/')}/{article.CategorySlug}/{article.Slug}";
            var authorUrl = $"{baseUrl.TrimEnd('/')}/author/{article.AuthorSlug}";
            var imageUrl = ToAbsoluteUrl(article.FeaturedImageXl, baseUrl);

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
                    url = $"{baseUrl.TrimEnd('/')}/tag/{tag.Slug}"
                }),

                image = imageUrl == null
                    ? Array.Empty<object>()
                    : new object[]
                    {
                        new
                        {
                            @type = "ImageObject",
                            url = imageUrl,
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
                    url = authorUrl
                },

                publisher = new
                {
                    @type = "Organization",
                    name = "Bol News",
                    url = baseUrl,
                    logo = new
                    {
                        @type = "ImageObject",
                        url = $"{baseUrl.TrimEnd('/')}/logo.png"
                    }
                },

                mainEntityOfPage = articleUrl
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
                url = $"{baseUrl.TrimEnd('/')}/category/{categorySlug}",

                mainEntity = new
                {
                    @type = "ItemList",
                    itemListElement = articles.Select((a, index) => new
                    {
                        @type = "ListItem",
                        position = index + 1,
                        url = $"{baseUrl.TrimEnd('/')}/{a.CategorySlug}/{a.Slug}"
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
                    url = $"{baseUrl.TrimEnd('/')}/logo.png"
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
                        name = article.CategoryName,
                        item = $"{baseUrl.TrimEnd('/')}/category/{article.CategorySlug}"
                    },
                    new
                    {
                        @type = "ListItem",
                        position = 3,
                        name = article.Title,
                        item = $"{baseUrl.TrimEnd('/')}/{article.CategorySlug}/{article.Slug}"
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
                        item = $"{baseUrl.TrimEnd('/')}/category/{categorySlug}"
                    }
                }
            };

            return JsonSerializer.Serialize(schema);
        }

        public string BuildAuthorSchema(AuthorPageVM author)
        {
            var authorSlug = string.IsNullOrWhiteSpace(author.Slug)
                ? Slugify(author.Name)
                : author.Slug;

            var schema = new
            {
                @context = "https://schema.org",
                @type = "Person",
                name = author.Name,
                description = author.Bio,
                image = ToAbsoluteUrl(author.ProfileImage, author.BaseUrl),
                url = $"{author.BaseUrl.TrimEnd('/')}/author/{authorSlug}",
                sameAs = Array.Empty<string>()
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
