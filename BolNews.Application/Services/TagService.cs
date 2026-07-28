using System.Text.Json;
using System.Text.RegularExpressions;
using BolNews.Application.Common.Helpers;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class TagService : ITagService
    {
        private readonly ITagRepository _tagRepository;

        public TagService(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository;
        }

        public string NormalizeName(string value)
            => Regex.Replace(value.Trim(), @"\s+", " ").ToUpperInvariant();

        public IReadOnlyList<string> ParseTagInput(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            var values = TryParseJsonArray(input)
                ?? input.Split(new[] { ',', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);

            return values
                .Select(x => Regex.Replace(x.Trim(), @"\s+", " "))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(25)
                .ToList();
        }

        public async Task<List<TagDto>> GetAllAsync()
            => (await _tagRepository.GetAllAsync()).Select(Map).ToList();

        public async Task<TagDto?> GetBySlugAsync(string slug)
        {
            var tag = await _tagRepository.GetBySlugAsync(slug);
            return tag == null ? null : Map(tag);
        }

        public async Task<List<TagDto>> GetArticleTagsAsync(int articleId)
            => (await _tagRepository.GetArticleTagsAsync(articleId)).Select(Map).ToList();

        public async Task<List<TagDto>> GetFeaturedImageTagsAsync(int articleId)
            => (await _tagRepository.GetFeaturedImageTagsAsync(articleId)).Select(Map).ToList();

        public async Task ReplaceArticleTagsAsync(int articleId, string? tagInput, string currentUserId)
        {
            var tags = await ResolveTagsAsync(ParseTagInput(tagInput), currentUserId);
            await _tagRepository.ReplaceArticleTagsAsync(articleId, tags, currentUserId);
            await _tagRepository.SaveChangesAsync();
        }

        public async Task ReplaceFeaturedImageTagsAsync(
            int articleId,
            string? tagInput,
            string? altText,
            string? caption,
            string? credit,
            string currentUserId)
        {
            var tags = await ResolveTagsAsync(ParseTagInput(tagInput), currentUserId);
            await _tagRepository.ReplaceFeaturedImageTagsAsync(articleId, tags, altText, caption, credit, currentUserId);
            await _tagRepository.SaveChangesAsync();
        }

        public async Task ReplaceMediaAssetTagsAsync(int mediaAssetId, string? tagInput, string currentUserId)
        {
            var tags = await ResolveTagsAsync(ParseTagInput(tagInput), currentUserId);
            await _tagRepository.ReplaceMediaAssetTagsAsync(mediaAssetId, tags, currentUserId);
            await _tagRepository.SaveChangesAsync();
        }

        private async Task<List<Tag>> ResolveTagsAsync(IReadOnlyList<string> tagNames, string currentUserId)
        {
            if (tagNames.Count == 0)
            {
                return new List<Tag>();
            }

            var normalizedNames = tagNames.Select(NormalizeName).ToList();
            var existingTags = await _tagRepository.GetByNormalizedNamesAsync(normalizedNames);
            var existingByNormalizedName = existingTags.ToDictionary(x => x.NormalizedName, x => x);
            var resolvedTags = new List<Tag>();
            var newTags = new List<Tag>();
            var knownSlugs = new HashSet<string>(
                (await _tagRepository.GetAllAsync()).Select(x => x.Slug),
                StringComparer.OrdinalIgnoreCase);

            foreach (var tagName in tagNames)
            {
                var normalizedName = NormalizeName(tagName);
                if (existingByNormalizedName.TryGetValue(normalizedName, out var existingTag))
                {
                    resolvedTags.Add(existingTag);
                    continue;
                }

                var newTag = new Tag
                {
                    Name = tagName,
                    NormalizedName = normalizedName,
                    Slug = GenerateUniqueSlug(tagName, knownSlugs),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId
                };

                newTags.Add(newTag);
                resolvedTags.Add(newTag);
                existingByNormalizedName[normalizedName] = newTag;
            }

            if (newTags.Count > 0)
            {
                await _tagRepository.AddTagsAsync(newTags);
                await _tagRepository.SaveChangesAsync();
            }

            return resolvedTags
                .GroupBy(x => x.NormalizedName)
                .Select(x => x.First())
                .ToList();
        }

        private static string GenerateUniqueSlug(string value, HashSet<string> knownSlugs)
        {
            var baseSlug = SlugHelper.GenerateSlug(value);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "tag";
            }

            var slug = baseSlug;
            var counter = 1;

            while (!knownSlugs.Add(slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            return slug;
        }

        private static string[]? TryParseJsonArray(string input)
        {
            try
            {
                return input.TrimStart().StartsWith("[", StringComparison.Ordinal)
                    ? JsonSerializer.Deserialize<string[]>(input)
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static TagDto Map(Tag tag)
            => new()
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug
            };
    }
}
