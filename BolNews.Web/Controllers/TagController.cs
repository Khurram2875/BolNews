using AutoMapper;
using BolNews.Application.Common;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class TagController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ITagService _tagService;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public TagController(
            IArticleService articleService,
            ITagService tagService,
            IMapper mapper,
            ICacheService cacheService)
        {
            _articleService = articleService;
            _tagService = tagService;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Details(string slug, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return NotFound();
            }

            var tag = await _tagService.GetBySlugAsync(slug);
            if (tag == null)
            {
                return NotFound();
            }

            const int pageSize = 10;
            var cacheKey = $"tag_{tag.Slug}_{page}";
            var articles = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async () => await _articleService.GetByTagSlugAsync(tag.Slug, page, pageSize),
                5);

            var vm = new CategorySectionVM
            {
                Articles = _mapper.Map<List<PublicArticleVM>>(articles),
                CategoryName = tag.Name,
                CategorySlug = tag.Slug,
                MetaTitle = $"{tag.Name} News",
                MetaDescription = $"Latest news tagged {tag.Name} on Bol News",
                Page = page,
                HasNextPage = articles.Count == pageSize
            };

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.HasNextPage = vm.HasNextPage;
            ViewBag.TagName = tag.Name;
            ViewBag.TagSlug = tag.Slug;
            ViewBag.MetaTitle = vm.MetaTitle;
            ViewBag.MetaDescription = vm.MetaDescription;
            ViewBag.CanonicalUrl = page == 1
                ? $"/tag/{tag.Slug}"
                : $"/tag/{tag.Slug}?page={page}";
            ViewBag.OgType = "website";
            ViewBag.OgImage = vm.Articles.FirstOrDefault()?.FeaturedImageXl;

            return View(vm);
        }
    }
}
