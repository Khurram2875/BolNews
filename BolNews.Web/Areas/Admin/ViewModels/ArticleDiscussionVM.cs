using BolNews.Application.DTOs;
using BolNews.Domain.Enums;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleDiscussionVM
    {
        public int ArticleId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public ArticleWorkflowStatus WorkflowStatus { get; set; }
        public string Thumbnail { get; set; }

        public List<ArticleDiscussionCommentDto> DiscussionComments
        { get; set; } = new();

        public string? NewComment { get; set; }
    }
}
