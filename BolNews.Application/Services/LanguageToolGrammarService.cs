using BolNews.Application.DTOs.Grammar;
using BolNews.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BolNews.Application.Services
{
    public sealed class LanguageToolGrammarService : IGrammarService
    {
        private readonly HttpClient _httpClient;

        public LanguageToolGrammarService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<GrammarIssueDto>> CheckAsync(
            string text,
            string language = "en-US",
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<GrammarIssueDto>();
            }

            using var formContent =
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["text"] = text,
                        ["language"] = language
                    });

            using var response =
                await _httpClient.PostAsync(
                    "v2/check",
                    formContent,
                    cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content
                    .ReadFromJsonAsync<LanguageToolResponse>(
                        cancellationToken: cancellationToken);

            if (result?.Matches is null)
            {
                return Array.Empty<GrammarIssueDto>();
            }

            var issues =
                new List<GrammarIssueDto>();

            for (var i = 0; i < result.Matches.Count; i++)
            {
                var match = result.Matches[i];

                if (match.Offset < 0 ||
                    match.Length <= 0 ||
                    match.Offset + match.Length > text.Length)
                {
                    continue;
                }

                var original =
                    text.Substring(
                        match.Offset,
                        match.Length);

                var replacement =
                    match.Replacements
                        .FirstOrDefault()
                        ?.Value
                    ?? string.Empty;

                issues.Add(
                    new GrammarIssueDto
                    {
                        Id = i + 1,

                        Offset = match.Offset,

                        Length = match.Length,

                        Original = original,

                        Replacement = replacement,

                        Message =
                            match.Message ?? string.Empty,

                        Category =
                            match.Rule?.Category?.Name
                            ?? "Grammar",

                        RuleId =
                            match.Rule?.Id
                            ?? string.Empty
                    });
            }

            return issues;
        }


        private sealed class LanguageToolResponse
        {
            [JsonPropertyName("matches")]
            public List<LanguageToolMatch> Matches { get; set; } = [];
        }


        private sealed class LanguageToolMatch
        {
            [JsonPropertyName("message")]
            public string? Message { get; set; }

            [JsonPropertyName("offset")]
            public int Offset { get; set; }

            [JsonPropertyName("length")]
            public int Length { get; set; }

            [JsonPropertyName("replacements")]
            public List<LanguageToolReplacement> Replacements { get; set; } = [];

            [JsonPropertyName("rule")]
            public LanguageToolRule? Rule { get; set; }
        }


        private sealed class LanguageToolReplacement
        {
            [JsonPropertyName("value")]
            public string Value { get; set; } = string.Empty;
        }


        private sealed class LanguageToolRule
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("category")]
            public LanguageToolCategory? Category { get; set; }
        }


        private sealed class LanguageToolCategory
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
