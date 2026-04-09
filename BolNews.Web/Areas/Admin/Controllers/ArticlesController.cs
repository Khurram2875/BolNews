using AutoMapper;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ArticlesController : Controller
    {
        private readonly IArticleService _articleService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthorService _authorService;
        private readonly IWebHostEnvironment _env;

        private readonly IMapper _mapper;

        public ArticlesController(
             IArticleService articleService,
             ICategoryService categoryService,
             IAuthorService authorService,
             IWebHostEnvironment env, IMapper mapper)
        {
            _articleService = articleService;
            _categoryService = categoryService;
            _authorService = authorService;
            _env = env;
            _mapper = mapper;
        }

        // GET: Admin/Articles
        public async Task<IActionResult> Index()
        {
            var dtos = await _articleService.GetAllAsync();
            var viewModels = _mapper.Map<List<ArticleVM>>(dtos);
            return View(viewModels);
        }

        // GET: Admin/Articles/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new ArticleVM());
        }
        private async Task PopulateDropdowns(int? categoryId = null, int? authorId = null)
        {
            var categories = await _categoryService.GetAllAsync();
            var authors = await _authorService.GetAllAsync();

            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.Authors = new SelectList(authors, "Id", "Name", authorId);
        }
        // POST: Admin/Articles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArticleVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var dto = _mapper.Map<ArticleDto>(model);
            await _articleService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Articles/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _articleService.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);
            await PopulateDropdowns();
            return View(model);
        }

        // POST: Admin/Articles/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ArticleVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model.CategoryId, model.AuthorId);
                return View(model);
            }
                

            var dto = _mapper.Map<ArticleDto>(model);
            await _articleService.UpdateAsync(dto);

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Articles/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _articleService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // Optional: Details view
        public async Task<IActionResult> Details(int id)
        {
            var dto = await _articleService.GetByIdAsync(id);
            if (dto == null)
                return NotFound();

            var model = _mapper.Map<ArticleVM>(dto);
            return View(model);
        }
    }
}