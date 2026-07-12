using BolNews.Application.DTOs;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleRevisionHistoryVM
    {
        public ArticleVM Article { get; set; } = new();

        public List<ArticleRevisionListItemVM> Revisions { get; set; } = new();
    }

    public class ArticleRevisionListItemVM
    {
        public ArticleRevisionDto Revision { get; set; } = new();

        public string ChangedByName { get; set; } = string.Empty;
    }
}
