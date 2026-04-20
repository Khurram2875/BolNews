using BolNews.Web.Interfaces;
using BolNews.Web.Models;

namespace BolNews.Web.Services
{
    public class HeadlineService : IHeadlineService
    {
        private readonly List<string> powerWords = new()
    {
        "shocking", "revealed", "breaking", "exclusive",
        "major", "critical", "surprising", "confirmed"
    };

        public HeadlineSuggestionResult Generate(string title)
        {
            var result = new HeadlineSuggestionResult
            {
                Original = title
            };

            if (string.IsNullOrWhiteSpace(title))
                return result;

            // 🔥 Suggestion 1: Add context
            result.Suggestions.Add($"{title} – What you need to know");

            // 🔥 Suggestion 2: Add emotion
            result.Suggestions.Add($"{title} as tensions rise globally");

            // 🔥 Suggestion 3: Question format
            result.Suggestions.Add($"What does this mean? {title}");

            // 🔥 Suggestion 4: Add power word
            var word = powerWords[new Random().Next(powerWords.Count)];
            result.Suggestions.Add($"{word.ToUpper()}: {title}");

            // 🔥 Suggestion 5: Improve length
            if (title.Length < 40)
            {
                result.Suggestions.Add($"{title} amid growing concerns worldwide");
            }

            return result;
        }
    }
}
