using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.Interfaces
{
    public interface IArticlePageService
    {
        Task<ArticleDetailsPageVM?> BuildDetailsPageAsync(string slug);
        Task TrackArticleEngagementAsync(int articleId, ISession session);
    }
}
