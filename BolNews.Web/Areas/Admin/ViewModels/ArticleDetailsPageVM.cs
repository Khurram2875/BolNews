using BolNews.Domain.Entities;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleDetailsPageVM
    {
        public PublicArticleVM Article { get; set; }
        public List<PublicArticleVM> RelatedArticles { get; set; }
        public Article Articles { get; set; }
        public string BaseUrl { get; set; }  // ✅ ADD THIS
        public string? MetaKeywords { get; set; }
        public string? ArticleSchemaJson { get; set; }
        public string? BreadcrumbSchemaJson { get; set; }
    }
}
