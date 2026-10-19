using BolNews.Application.Interfaces;
using BolNews.Application.Common;
using BolNews.Domain.Common;
using BolNews.Domain.Enums;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Roles.Admin + "," + Roles.Editor + "," + Roles.SubEditor)]
public class InternalLinksController(
    IArticleService articleService,
    IArticleInternalLinkRuleService ruleService,
    ICacheService cacheService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? articleId)
    {
        var model = new InternalLinksManageVM();
        if (!articleId.HasValue || articleId.Value <= 0)
            return View(model);

        var article = await articleService.GetEntityByIdAsync(articleId.Value);
        if (article == null)
            return NotFound();

        model.SourceArticleId = article.Id;
        model.SourceArticleTitle = article.Title;
        model.Rules = (await ruleService.GetForSourceAsync(article.Id))
            .Select(rule => new InternalLinkRuleRowVM
            {
                Id = rule.Id,
                RuleType = rule.RuleType,
                AnchorText = rule.AnchorText,
                DestinationLabel = rule.TargetArticle?.Title ?? rule.TargetUrl ?? "Unknown destination",
                DestinationUrl = rule.TargetArticle?.Category?.Slug is string categorySlug &&
                                  !string.IsNullOrWhiteSpace(rule.TargetArticle.Slug)
                    ? $"/{categorySlug}/{rule.TargetArticle.Slug}"
                    : rule.TargetUrl
            })
            .ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchArticles(string? term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2)
            return Json(Array.Empty<object>());

        var articles = await articleService.SearchArticlesAsync(term.Trim(), 12);
        return Json(articles.Select(article => new { id = article.Id, text = article.Title }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddManualLink(AddManualInternalLinkVM input)
    {
        try
        {
            await ruleService.AddManualLinkAsync(
                input.SourceArticleId,
                input.AnchorText,
                input.TargetArticleId,
                input.TargetUrl);
            await InvalidateSourceArticleCacheAsync(input.SourceArticleId);
            TempData["Success"] = "Manual internal link saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { articleId = input.SourceArticleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddExclusion(AddInternalLinkExclusionVM input)
    {
        try
        {
            await ruleService.AddExclusionAsync(input.SourceArticleId, input.TargetArticleId);
            await InvalidateSourceArticleCacheAsync(input.SourceArticleId);
            TempData["Success"] = "Automatic link exclusion saved.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { articleId = input.SourceArticleId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRule(int sourceArticleId, int ruleId)
    {
        if (await ruleService.DeleteAsync(sourceArticleId, ruleId))
        {
            await InvalidateSourceArticleCacheAsync(sourceArticleId);
            TempData["Success"] = "Internal link rule removed.";
        }
        else
        {
            TempData["Error"] = "That internal link rule could not be found.";
        }

        return RedirectToAction(nameof(Index), new { articleId = sourceArticleId });
    }

    private async Task InvalidateSourceArticleCacheAsync(int sourceArticleId)
    {
        cacheService.Remove($"article_content_v5_{sourceArticleId}");
        var sourceArticle = await articleService.GetEntityByIdAsync(sourceArticleId);
        if (sourceArticle != null)
            cacheService.Remove(CacheKeys.ArticlePage(sourceArticle.Slug));
    }
}
