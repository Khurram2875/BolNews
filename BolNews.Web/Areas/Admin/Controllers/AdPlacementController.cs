using BolNews.Application.Interfaces;
using BolNews.Application.Services;
using BolNews.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,AdManager")]
    public class AdPlacementController : Controller
    {
        private readonly IAdService _service;
        public AdPlacementController(IAdService service)
        {
            _service = service;
        }
        public async Task<IActionResult> Index()
        {
            var ads = await _service.GetAllAsync(); // assuming generic repo method

            return View(ads);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdPlacement model)
        {
            if (!ModelState.IsValid)
                return View(model);

            model.CreatedAt = DateTime.UtcNow;

            await _service.AddAsync(model);
            await _service.SaveAsync(); // if your repo supports it

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var ad = await _service.GetByIdAsync(id);

            if (ad == null)
                return NotFound();

            return View(ad);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdPlacement model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existing = await _service.GetByIdAsync(model.Id);

            if (existing == null)
                return NotFound();

            existing.Name = model.Name;
            existing.PlacementKey = model.PlacementKey;
            existing.AdCode = model.AdCode;
            existing.IsEnabled = model.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow;

            await _service.UpdateAsync(existing);
            await _service.SaveAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Delete(int id)
        {
            var ad = await _service.GetByIdAsync(id);

            if (ad == null)
                return NotFound();

            await _service.DeleteAsync(ad);
            await _service.SaveAsync();

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, bool isEnabled)
        {
            await _service.ToggleStatusAsync(id, isEnabled);

            return Json(new
            {
                success = true,
                message = isEnabled ? "Ad Enabled" : "Ad Disabled"
            });
        }
    }
}
