using BolNews.Application.DTOs.Grammar;
using BolNews.Application.Interfaces;
using BolNews.Web.Interfaces;

namespace BolNews.Web.Services
{
    public sealed class GeminiGrammarService : IGrammarService
    {
        private readonly IGeminiService _geminiService;

        public GeminiGrammarService(IGeminiService geminiService)
        {
            _geminiService = geminiService;
        }

        public Task<IReadOnlyList<GrammarIssueDto>> CheckAsync(
            string text,
            string language = "en-US",
            CancellationToken cancellationToken = default)
        {
            return _geminiService.CheckGrammarAsync(
                text,
                language,
                cancellationToken);
        }
    }
}
