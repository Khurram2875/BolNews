using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Models;

namespace BolNews.Web.Interfaces
{
    public interface IDiscoverService
    {
        DiscoverScoreResult Evaluate(PublicArticleVM article);
    }
}
