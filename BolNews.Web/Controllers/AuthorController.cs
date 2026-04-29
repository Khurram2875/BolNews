using BolNews.Application.Interfaces;
using BolNews.Web.Areas.Admin.ViewModels;
using BolNews.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers
{
    public class AuthorController : Controller
    {
        private readonly IAuthorService _authorService;
        private readonly IArticleService _articleService;
        private readonly IUrlService _urlService;
        private readonly ISeoService _seoService;

        public AuthorController(
            IAuthorService authorService,
            IArticleService articleService,
            IUrlService urlService,
            ISeoService seoService)
        {
            _authorService = authorService;
            _articleService = articleService;
            _urlService = urlService;
            _seoService = seoService;
        }

        public async Task<IActionResult> Details(string authorSlug)
        {
            var author = await _authorService.GetBySlugAsync(authorSlug);

            if (author == null)
                return NotFound();

            var articles = await _articleService.GetByAuthorAsync(author.Id);

            var baseUrl = _urlService.GetBaseUrl();

            var vm = new AuthorPageVM
            {
                Name = author.Name,
                Bio = author.Bio,
                ProfileImage = author.ProfileImageUrl,
                Articles = articles,
                BaseUrl = baseUrl
            };

            vm.SchemaJson = _seoService.BuildAuthorSchema(vm);

            return View(vm);
        }
    }
}
