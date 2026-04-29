using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    [Route("analytics")]
    public class AnalyticsController : Controller
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpPost("click/{id}")]
        public async Task<IActionResult> TrackClick(int id)
        {
            await _analyticsService.TrackClickAsync(id);
            return Ok();
        }
    }
}
