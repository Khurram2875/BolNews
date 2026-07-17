using BolNews.Application.DTOs.Grammar;
using BolNews.Web.Interfaces;
using Google.GenAI;
using System.Text.Json;

namespace BolNews.Web.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly Client _client;
        private const string ModelName = "gemini-2.5-flash";

        public GeminiService(IConfiguration configuration)
        {
            string apiKey = configuration["Gemini:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API Key is missing in configuration.");

            _client = new Client(apiKey: apiKey);
        }

        public async Task<(string MetaTitle, string MetaDescription)> GenerateMetadataAsync(
            string title,
            string summary,
            string category)
        {
            string prompt = $@"
               You are an SEO expert and engineer for the enterprise news agency BolNews. 
                Based on the provided Article Title, Summary, and Category, generate an original, highly optimized SEO Meta Title and SEO Meta Description.
    
                CRITICAL QUALITY CONTROL (DO NOT MIRROR INPUT):
                - Do NOT simply copy and paste the provided Article Title as the Meta Title. You must rewrite, condense, or reformat it for search performance.
                - Do NOT reuse the provided Summary verbatim as the Meta Description. You must synthesize and compress it into a punchy snippet.
    
                CRITICAL REQUIREMENTS:
                1. Meta Title:
                   - Must be between 50 and 60 characters max (do not exceed 60).
                   - Front-load primary keywords, key entities (people, places, brands), or critical data points near the beginning for search indexing.
                   - Append brand relevance (e.g., '| BolNews') if character space permits naturally.
                2. Meta Description:
                   - Must be a compelling summary between 130 and 155 characters max (never exceed 160).
                   - Use active, punchy verbs to summarize the core 'hook' or event.
                   - Include relevant localized context or secondary entities to improve discoverability.
                3. Output Format:
                   - You MUST respond ONLY with a valid raw JSON object. 
                   - Do not wrap it in markdown code blocks like ```json ... 
            ```. 
                   - Do not include any conversational filler, introductory text, or trailing notes.
    
                Expected JSON format:
                {{
                    ""metaTitle"": ""Your generated title"",
                    ""metaDescription"": ""Your generated description""
                }}

                Article Data:
                - Title: {title}
                - Summary: {summary}
                - Category: {category}
            ";

            try
            {
                var response = await _client.Models.GenerateContentAsync(
                    model: ModelName,
                    contents: prompt
                );

                var jsonResponse = ExtractResponseText(response);

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return ($"{title} | BolNews", summary);
                }

                jsonResponse = CleanJsonResponse(jsonResponse);

                var result = JsonSerializer.Deserialize<GeminiMetaResponseDto>(
                    jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return (result?.MetaTitle ?? title, result?.MetaDescription ?? summary);
            }
            catch
            {
                return ($"{title} | BolNews", summary);
            }
        }

        public async Task<IReadOnlyList<GrammarIssueDto>> CheckGrammarAsync(
            string text,
            string language = "en-US",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<GrammarIssueDto>();
            }

            var prompt = $@"
                You are a professional newsroom proofreader for BolNews.
                Review the article text for spelling, grammar, punctuation, word choice, and clarity issues.

                CRITICAL RULES:
                - Do not rewrite the full article.
                - Return only individual corrections.
                - Preserve the article's meaning, names, quotes, numbers, and journalistic tone.
                - Use {language} language conventions.
                - The offset must be the zero-based character index in the exact article text.
                - The length must be the exact character count of the original text span.
                - The original value must exactly match the text span at offset/length.
                - If there are no issues, return an empty issues array.
                - Respond only with a valid raw JSON object. Do not use markdown.

                Expected JSON:
                {{
                    ""issues"": [
                        {{
                            ""offset"": 0,
                            ""length"": 0,
                            ""original"": ""text to replace"",
                            ""replacement"": ""corrected text"",
                            ""message"": ""short explanation"",
                            ""category"": ""Grammar"",
                            ""ruleId"": ""GEMINI_GRAMMAR""
                        }}
                    ]
                }}

                Article text:
                {JsonSerializer.Serialize(text)}
            ";

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var response = await _client.Models.GenerateContentAsync(
                    model: ModelName,
                    contents: prompt
                );

                cancellationToken.ThrowIfCancellationRequested();

                var jsonResponse = ExtractResponseText(response);

                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return Array.Empty<GrammarIssueDto>();
                }

                jsonResponse = CleanJsonResponse(jsonResponse);

                var result = JsonSerializer.Deserialize<GeminiGrammarResponseDto>(
                    jsonResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return NormalizeGrammarIssues(result?.Issues, text);
            }
            catch
            {
                return Array.Empty<GrammarIssueDto>();
            }
        }

        private static IReadOnlyList<GrammarIssueDto> NormalizeGrammarIssues(
            IReadOnlyList<GeminiGrammarIssueDto>? issues,
            string sourceText)
        {
            if (issues is null || issues.Count == 0)
            {
                return Array.Empty<GrammarIssueDto>();
            }

            var normalizedIssues = new List<GrammarIssueDto>();
            var usedRanges = new HashSet<string>();

            foreach (var issue in issues)
            {
                if (string.IsNullOrWhiteSpace(issue.Original) ||
                    string.IsNullOrWhiteSpace(issue.Replacement))
                {
                    continue;
                }

                var offset = issue.Offset;
                var length = issue.Length;
                var original = issue.Original;

                if (!IsValidRange(sourceText, offset, length, original))
                {
                    offset = sourceText.IndexOf(
                        original,
                        StringComparison.Ordinal);

                    length = original.Length;
                }

                if (!IsValidRange(sourceText, offset, length, original))
                {
                    continue;
                }

                var replacement = issue.Replacement.Trim();

                if (string.Equals(original, replacement, StringComparison.Ordinal))
                {
                    continue;
                }

                var rangeKey = $"{offset}:{length}";

                if (!usedRanges.Add(rangeKey))
                {
                    continue;
                }

                normalizedIssues.Add(
                    new GrammarIssueDto
                    {
                        Id = normalizedIssues.Count + 1,
                        Offset = offset,
                        Length = length,
                        Original = sourceText.Substring(offset, length),
                        Replacement = replacement,
                        Message = string.IsNullOrWhiteSpace(issue.Message)
                            ? "Suggested proofreading correction."
                            : issue.Message.Trim(),
                        Category = string.IsNullOrWhiteSpace(issue.Category)
                            ? "Grammar"
                            : issue.Category.Trim(),
                        RuleId = string.IsNullOrWhiteSpace(issue.RuleId)
                            ? "GEMINI_PROOFREADING"
                            : issue.RuleId.Trim()
                    });
            }

            return normalizedIssues;
        }

        private static bool IsValidRange(
            string sourceText,
            int offset,
            int length,
            string original)
        {
            return offset >= 0 &&
                   length > 0 &&
                   offset + length <= sourceText.Length &&
                   string.Equals(
                       sourceText.Substring(offset, length),
                       original,
                       StringComparison.Ordinal);
        }

        private static string CleanJsonResponse(string response)
        {
            return response
                .Replace("```json", string.Empty)
                .Replace("```", string.Empty)
                .Trim();
        }

        private static string? ExtractResponseText(dynamic response)
        {
            return response?.Candidates?[0]?.Content?.Parts?[0]?.Text;
        }
    }

    internal class GeminiMetaResponseDto
    {
        public string MetaTitle { get; set; } = string.Empty;

        public string MetaDescription { get; set; } = string.Empty;
    }

    internal sealed class GeminiGrammarResponseDto
    {
        public List<GeminiGrammarIssueDto> Issues { get; set; } = [];
    }

    internal sealed class GeminiGrammarIssueDto
    {
        public int Offset { get; set; }

        public int Length { get; set; }

        public string Original { get; set; } = string.Empty;

        public string Replacement { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string RuleId { get; set; } = string.Empty;
    }
}
