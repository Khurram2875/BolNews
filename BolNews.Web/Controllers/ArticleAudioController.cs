using BolNews.Web.Services.Tts;
using Microsoft.AspNetCore.Mvc;

namespace BolNews.Web.Controllers;

public sealed class ArticleAudioController(ArticleAudioService audio) : Controller
{
    /// <summary>Returns the URL of the article's audio, creating it on the first request. Any non-200 answer makes the page use browser voices.</summary>
    [HttpGet("/article-audio/{id:int}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await audio.GetOrCreateAsync(id, cancellationToken);
            return result is null ? NotFound() : Ok(new { url = result.Url, characters = result.Characters });
        }
        catch (TtsException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
