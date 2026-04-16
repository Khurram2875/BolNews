namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class ArticleDetailsPageVM
    {
        public PublicArticleVM Article { get; set; }
        public List<PublicArticleVM> RelatedArticles { get; set; }
    }
}
