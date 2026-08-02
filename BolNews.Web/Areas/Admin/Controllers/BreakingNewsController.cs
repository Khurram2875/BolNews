using AutoMapper;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Domain.Entities;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Areas.Admin.ViewModels.BreakingNews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin + "," + Roles.Editor + "," + Roles.SubEditor)]
    public class BreakingNewsController : Controller
    {
        private readonly IBreakingNewsService _breakingNewsService;
        private readonly IArticleService _articleService;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> _userManager;

        public BreakingNewsController(IBreakingNewsService breakingNewsService, IArticleService articleService, IMapper mapper, UserManager<ApplicationUser> userManager)
        {
            _breakingNewsService = breakingNewsService;
            _articleService = articleService;
            _mapper = mapper;
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var news = await _breakingNewsService.GetAllAsync();

            var vm = new BreakingNewsListVM
            {
                BreakingNews = _mapper.Map<List<BreakingNewsVM>>(news)
            };

            return View(vm);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateBreakingNewsVM());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBreakingNewsVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = _mapper.Map<BreakingNews>(vm);

            var userId = _userManager.GetUserId(User)!;

            await _breakingNewsService.CreateAsync(entity, userId);

            TempData["Success"] = "Breaking News created successfully.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var news = await _breakingNewsService.GetByIdAsync(id);

            if (news == null)
                return NotFound();

            var vm = _mapper.Map<EditBreakingNewsVM>(news);

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditBreakingNewsVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = await _breakingNewsService.GetByIdAsync(vm.Id);

            var userId = _userManager.GetUserId(User)!;

            if (entity == null)
                return NotFound();

            _mapper.Map(vm, entity);

            await _breakingNewsService.UpdateAsync(entity, userId);

            TempData["Success"] = "Breaking News updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            await _breakingNewsService.DeleteAsync(id, userId);

            return Json(new
            {
                success = true
            });
        }
        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            await _breakingNewsService.ActivateAsync(id, userId);

            return Json(new
            {
                success = true
            });
        }
        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            var userId = _userManager.GetUserId(User)!;

            await _breakingNewsService.DeactivateAsync(id, userId);

            return Json(new
            {
                success = true
            });
        }
        [HttpGet]
        public async Task<IActionResult> SearchArticles(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(Array.Empty<object>());

            var articles = await _articleService.SearchArticlesAsync(term);

            var result = articles.Select(x => new Select2ItemVM
            {
                Id = x.Id,
                Text = x.Title
            });

            return Json(result);
        }
    }
}
