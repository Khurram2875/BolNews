using BolNews.Application.DTOs.Grammar;

namespace BolNews.Web.Interfaces
{
    public interface IGeminiService
    {
        Task<(string MetaTitle, string MetaDescription)> GenerateMetadataAsync(
            string title,
            string summary,
            string category);

        Task<IReadOnlyList<GrammarIssueDto>> CheckGrammarAsync(
            string text,
            string language = "en-US",
            CancellationToken cancellationToken = default);
    }
}
