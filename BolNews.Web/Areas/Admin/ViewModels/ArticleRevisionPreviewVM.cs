using BolNews.Application.DTOs;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleRevisionPreviewVM
    {
        public ArticleVM Article { get; set; } = new();

        public ArticleRevisionDto Revision { get; set; } = new();

        public string ChangedByName { get; set; } = string.Empty;
    }
}
