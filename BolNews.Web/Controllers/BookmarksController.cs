using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;

namespace BolNews.Web.Controllers;

public class BookmarksController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IMapper _mapper;

    public BookmarksController(IArticleService articleService, IMapper mapper)
    {
        _articleService = articleService;
        _mapper = mapper;
    }

    [HttpGet("/saved-articles")]
    public IActionResult Index() => View();

    [HttpGet("/saved-articles/recommended")]
    public async Task<IActionResult> Recommended(
        [FromQuery] string[]? categories,
        [FromQuery] string[]? savedTitles,
        [FromQuery] int[]? excludeIds)
    {
        var categoryNames = (categories ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var excluded = (excludeIds ?? Array.Empty<int>()).ToHashSet();
        var savedTitleTerms = (savedTitles ?? Array.Empty<string>())
            .Take(30)
            .SelectMany(GetMeaningfulTerms)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Category is the hard relevance boundary. Text similarity ranks stories
        // within the visitor's saved categories, but cannot introduce other ones.
        if (categoryNames.Count == 0)
            return Json(Array.Empty<PublicArticleVM>());

        var recentArticles = await _articleService.GetLatestArticlesAsync(200);
        var recommendations = recentArticles
            .Where(article => !excluded.Contains(article.Id)
                && article.Category != null
                && categoryNames.Contains(article.Category.Name))
            .Select(article => new
            {
                Article = article,
                Score = ScoreRecommendation(article, savedTitleTerms)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Article.PublishedAt)
            .Take(12)
            .Select(candidate => candidate.Article)
            .ToList();

        return Json(_mapper.Map<List<PublicArticleVM>>(recommendations));
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "after", "against", "among", "also", "been", "before", "being", "between",
        "could", "from", "have", "into", "more", "most", "over", "said", "says", "than",
        "that", "their", "there", "these", "they", "this", "those", "through", "under",
        "were", "when", "where", "which", "while", "with", "would", "your"
    };

    private static IEnumerable<string> GetMeaningfulTerms(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        return System.Text.RegularExpressions.Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}]{3,}")
            .Select(match => match.Value)
            .Where(term => !StopWords.Contains(term));
    }

    private static int ScoreRecommendation(
        BolNews.Domain.Entities.Article article,
        HashSet<string> savedTitleTerms)
    {
        // Every same-category candidate gets a baseline so category relevance
        // remains useful even if its title uses different wording.
        var score = 1;
        var titleTerms = GetMeaningfulTerms(article.Title).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var summaryTerms = GetMeaningfulTerms(article.Summary).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tagTerms = (article.ArticleTags ?? Array.Empty<BolNews.Domain.Entities.ArticleTag>())
            .Where(articleTag => articleTag.Tag != null)
            .SelectMany(articleTag => GetMeaningfulTerms(articleTag.Tag.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        score += titleTerms.Count(savedTitleTerms.Contains) * 5;
        score += tagTerms.Count(savedTitleTerms.Contains) * 4;
        score += summaryTerms.Count(savedTitleTerms.Contains);
        return score;
    }
}
