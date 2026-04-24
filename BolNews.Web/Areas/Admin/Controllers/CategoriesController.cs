using AutoMapper;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IMapper _mapper;

        public CategoriesController(ICategoryService categoryService, IMapper mapper)
        {
            _categoryService = categoryService;
            _mapper = mapper;
        }

        private async Task PopulateParentCategories(int? selectedId = null)
        {
            var categories = await _categoryService.GetAllAsync();

            ViewBag.ParentCategories = new SelectList(
                categories,
                "Id",
                "Name",
                selectedId
            );
        }

        // GET: Index
        public async Task<IActionResult> Index()
        {
            var dtos = await _categoryService.GetAllAsync();
            var flatList = _mapper.Map<List<CategoryVM2>>(dtos);
            var tree = BuildTree(flatList);

            return View(tree);
        }
        private List<CategoryVM2> BuildTree(List<CategoryVM2> categories, int? parentId = null)
        {
            return categories
                .Where(c => c.ParentCategoryId == parentId)
                .Select(c => new CategoryVM2
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ParentCategoryId = c.ParentCategoryId,
                    Children = BuildTree(categories, c.Id)
                })
                .ToList();
        }
        // GET: Create
        public async Task<IActionResult> Create()
        {
            await PopulateParentCategories();
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateParentCategories(model.ParentCategoryId);
                return View(model);
            }

            var dto = _mapper.Map<CategoryDto>(model);
            await _categoryService.CreateAsync(dto);

            return RedirectToAction(nameof(Index));
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await _categoryService.GetByIdAsync(id);
            if (dto == null) return NotFound();

            var vm = _mapper.Map<CategoryVM>(dto);

            await PopulateParentCategories(vm.ParentCategoryId);

            return View(vm);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateParentCategories(model.ParentCategoryId);
                return View(model);
            }

            var dto = _mapper.Map<CategoryDto>(model);
            await _categoryService.UpdateAsync(dto);

            return RedirectToAction(nameof(Index));
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _categoryService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
