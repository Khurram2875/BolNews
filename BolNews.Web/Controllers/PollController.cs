using BolNews.Domain.Entities;
using BolNews.Persistence.Context;
using BolNews.Web.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Controllers;

public class PollController(AppDbContext db) : Controller
{
    /// <summary>
    /// Casts one vote. One vote per visitor per poll: a long-lived cookie identifies the visitor and the
    /// database enforces uniqueness; a per-IP cap stops scripted abuse. Clearing cookies or using another
    /// browser can bypass the cookie check, true one-person-one-vote would need sign-in.
    /// </summary>
    [HttpPost("/polls/{id:int}/vote")]
    public async Task<IActionResult> Vote(int id, [FromForm] int optionId)
    {
        var poll = await db.Polls.AsNoTracking().Where(x => x.Id == id && x.IsActive)
            .Select(x => new { x.EndsAt }).FirstOrDefaultAsync();
        if (poll == null) return NotFound(new { ok = false, message = "This poll is not available." });
        if (DateTime.UtcNow >= poll.EndsAt)
            return await Respond(id, 409, "Voting has ended for this poll.");

        if (!await db.PollOptions.AnyAsync(o => o.Id == optionId && o.PollId == id))
            return BadRequest(new { ok = false, message = "Please choose one of the options." });

        var voterId = Request.Cookies[PollHelper.VoterCookie];
        if (string.IsNullOrWhiteSpace(voterId) || voterId.Length > 64)
        {
            voterId = Guid.NewGuid().ToString("N");
            Response.Cookies.Append(PollHelper.VoterCookie, voterId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });
        }

        var voterHash = PollHelper.VoterHash(id, voterId);
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        var ipHash = PollHelper.IpHash(id, remoteIp?.ToString());

        if (await db.PollVotes.AnyAsync(v => v.PollId == id && v.VoterHash == voterHash))
            return await Respond(id, 409, "You have already voted in this poll.", voterId);
        if (!PollHelper.IsMeaninglessIp(remoteIp))
        {
            var since = DateTime.UtcNow.AddMinutes(-PollHelper.IpWindowMinutes);
            if (await db.PollVotes.CountAsync(v => v.PollId == id && v.IpHash == ipHash && v.CreatedAt >= since) >= PollHelper.MaxVotesPerIp)
                return await Respond(id, 429, "Too many votes from this network just now. Please try again in a few minutes.", voterId);
        }

        db.PollVotes.Add(new PollVote { PollId = id, OptionId = optionId, VoterHash = voterHash, IpHash = ipHash, CreatedAt = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two requests raced: the unique index let only one through.
            return await Respond(id, 409, "You have already voted in this poll.", voterId);
        }
        return await Respond(id, 200, "Thanks for voting!", voterId, voted: true);
    }

    private async Task<IActionResult> Respond(int pollId, int status, string message, string? voterId = null, bool voted = false)
    {
        var card = await PollHelper.BuildCardAsync(db, pollId, voterId);
        Response.StatusCode = status;
        return Json(new
        {
            ok = status == 200,
            message,
            voted = voted || (card?.HasVoted ?? false),
            ended = card?.Ended ?? false,
            options = card?.Options.Select(o => new { id = o.Id, percent = o.Percent }) ?? Enumerable.Empty<object>()
        });
    }
}
