using BolNews.Application.DTOs;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleRevisionCompareVM
    {
        public ArticleVM Article { get; set; } = new();

        public ArticleRevisionCompareDto Comparison { get; set; } = new();

        public string FromChangedByName { get; set; } = string.Empty;
    }
}
