using BolNews.Persistence.Context;
using BolNews.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.ViewComponents;

/// <summary>
/// Poll card for any page:  @await Component.InvokeAsync("PollCard")                 (newest poll, open ones first)
///                          @await Component.InvokeAsync("PollCard", new { pollId = 3 })   (a specific poll)
/// Optional wrapperClass (e.g. "col-lg-4") wraps the card in a div that is only output when a poll is active,
/// so the column/space collapses completely when no poll is active.
/// Renders nothing when there is no active poll, or when the polls table does not exist yet.
/// </summary>
public class PollCardViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int? pollId = null, string? wrapperClass = null)
    {
        try
        {
            var id = pollId;
            if (id == null)
            {
                var now = DateTime.UtcNow;
                id = await db.Polls.AsNoTracking().Where(x => x.IsActive)
                    .OrderByDescending(x => x.EndsAt > now).ThenByDescending(x => x.CreatedAt)
                    .Select(x => (int?)x.Id).FirstOrDefaultAsync();
            }
            if (id == null) return Content(string.Empty);

            var voterId = HttpContext.Request.Cookies[PollHelper.VoterCookie];
            var model = await PollHelper.BuildCardAsync(db, id.Value, voterId);
            if (model == null || model.Options.Count < 2) return Content(string.Empty);
            ViewData["WrapperClass"] = wrapperClass;
            return View(model);
        }
        catch (Exception)
        {
            // A missing table (migration not applied yet) must never break the host page.
            return Content(string.Empty);
        }
    }
}
