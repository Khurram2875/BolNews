using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Common;
using BolNews.Web.Areas.Admin.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BolNews.Domain.Entities;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = Roles.Admin + "," + Roles.Editor)]
    [Area("Admin")]
    public class ReportersController : Controller
    {
        private readonly IReporterService _reporterService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportersController(
            IReporterService reporterService,
            UserManager<ApplicationUser> userManager)
        {
            _reporterService = reporterService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var reporters = await _reporterService.GetAllAsync();
            var model = reporters.Select(ToVm).ToList();

            return View(model);
        }

        public IActionResult Create()
        {
            return View(new ReporterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReporterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _reporterService.CreateAsync(ToDto(model), CurrentUserId());

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var reporter = await _reporterService.GetByIdAsync(id);

            if (reporter == null)
                return NotFound();

            return View(ToVm(reporter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ReporterVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _reporterService.UpdateAsync(ToDto(model), CurrentUserId());

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _reporterService.DeleteAsync(id, CurrentUserId());

            return RedirectToAction(nameof(Index));
        }

        private string CurrentUserId()
        {
            return _userManager.GetUserId(User) ?? string.Empty;
        }

        private static ReporterVM ToVm(ReporterDto dto)
        {
            return new ReporterVM
            {
                Id = dto.Id,
                Name = dto.Name,
                SourceName = dto.SourceName,
                Slug = dto.Slug
            };
        }

        private static ReporterDto ToDto(ReporterVM model)
        {
            return new ReporterDto
            {
                Id = model.Id,
                Name = model.Name,
                SourceName = model.SourceName,
                Slug = model.Slug
            };
        }
    }
}
