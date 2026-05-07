using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.Interfaces
{
    public interface IArticlePageService
    {
        Task<ArticleDetailsPageVM?> BuildDetailsPageAsync(string categorySlug, string slug, ISession session);
    }
}
