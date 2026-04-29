using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.Interfaces
{
  
        public interface ISeoService
        {
            string BuildArticleSchema(PublicArticleVM article, string baseUrl);

            string BuildCategorySchema(
                string categoryName,
                string categorySlug,
                string metaDescription,
                List<PublicArticleVM> articles,
                string baseUrl
            );

            string BuildOrganizationSchema(string baseUrl);

            string BuildBreadcrumb(PublicArticleVM article, string baseUrl);

            string BuildCategoryBreadcrumb(string categoryName, string categorySlug, string baseUrl);
            string BuildAuthorSchema(AuthorPageVM author);
        }
    
}
