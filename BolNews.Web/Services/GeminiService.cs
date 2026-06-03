using BolNews.Web.Interfaces;
using Google.GenAI;
using System.Text.Json;

namespace BolNews.Web.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly Client _client;
        private const string ModelName = "gemini-2.5-flash"; // Recommended fast model for text tasks

        public GeminiService(IConfiguration configuration)
        {
            // Pull your API key securely from appsettings.json or user secrets
            string apiKey = configuration["Gemini:ApiKey"]
                ?? throw new ArgumentNullException("Gemini API Key is missing in configuration.");

            _client = new Client(apiKey: apiKey);
        }

        public async Task<(string MetaTitle, string MetaDescription)> GenerateMetadataAsync(string title, string summary, string category)
        {
            // Crafting a system/user prompt instructing JSON output constraints
            string prompt = $@"
                You are an SEO expert for a news agency called BolNews. 
                Based on the provided Article Title, Summary, and Category, generate an optimized SEO Meta Title and SEO Meta Description.
                
                CRITICAL REQUIREMENTS:
                1. Meta Title must be under 60 characters and include brand relevance if appropriate.
                2. Meta Description must be a compelling summary under 160 characters.
                3. You MUST respond ONLY with a valid raw JSON object. Do not wrap it in markdown code blocks like ```json.
                
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

                // Extract the string generation text
                string jsonResponse = response.Candidates[0].Content.Parts[0].Text;

                // Strip accidental markdown blocks if Gemini added them anyway
                jsonResponse = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                // Deserialize directly into our data structure
                var result = JsonSerializer.Deserialize<GeminiMetaResponseDto>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (result?.MetaTitle ?? title, result?.MetaDescription ?? summary);
            }
            catch (Exception ex)
            {
                // Fallback gracefully if AI or parsing fails so it doesn't crash the editor's UI workflow
                // Log exception (ex) here as needed.
                return ($"{title} | BolNews", summary);
            }
        }
    }

    // internal DTO helper for parsing
    internal class GeminiMetaResponseDto
    {
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
    }
}
