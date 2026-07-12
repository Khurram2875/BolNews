using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface IArticleDiffService
    {
        ArticleRevisionCompareDto Compare(
            ArticleRevisionDto fromRevision,
            ArticleDto toArticle,
            string toLabel);

        ArticleRevisionCompareDto Compare(
            ArticleRevisionDto fromRevision,
            ArticleRevisionDto toRevision,
            string toLabel);
    }
}
