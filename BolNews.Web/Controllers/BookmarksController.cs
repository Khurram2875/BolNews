using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers;

public class BookmarksController : Controller
{
    [HttpGet("/saved-articles")]
    public IActionResult Index() => View();
}
