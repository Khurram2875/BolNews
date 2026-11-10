using System.Security.Cryptography;
using System.Text;
using BolNews.Domain.Enums;
using BolNews.Persistence.Context;
using BolNews.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BolNews.Web.Helpers;

/// <summary>Voter identity, result maths and card building for the Polls feature.</summary>
public static class PollHelper
{
    public const string VoterCookie = "bn_voter";
    /// <summary>Burst limit: votes allowed from one IP address per poll within <see cref="IpWindowMinutes"/>.
    /// It only slows scripts down; the real one-vote-per-visitor rule is the cookie. It is deliberately generous
    /// because many mobile users share one public IP (carrier NAT) and a misconfigured proxy shows every visitor as one IP.</summary>
    public const int MaxVotesPerIp = 40;
    public const int IpWindowMinutes = 10;

    /// <summary>Loopback / private / missing addresses say nothing about the visitor (reverse proxy not forwarding the real IP), so no IP cap is applied to them.</summary>
    public static bool IsMeaninglessIp(System.Net.IPAddress? ip)
    {
        if (ip == null || System.Net.IPAddress.IsLoopback(ip)) return true;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        var b = ip.GetAddressBytes();
        if (b.Length == 4) return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
        return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || (b[0] & 0xFE) == 0xFC; // fe80::, site-local, fc00::/7
    }

    public static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static string VoterHash(int pollId, string voterId) => Sha256($"poll:{pollId}:voter:{voterId}");
    public static string IpHash(int pollId, string? ip) => Sha256($"poll:{pollId}:ip:{ip ?? "unknown"}");

    public static string TypeLabel(PollType type) => type switch
    {
        PollType.Versus => "Versus",
        PollType.TrueFalse => "True or False",
        _ => "Poll"
    };

    /// <summary>Whole-number percentages that always add up to 100 (largest remainder), or all zeros with no votes.</summary>
    public static int[] Percentages(IReadOnlyList<int> counts)
    {
        var result = new int[counts.Count];
        var total = counts.Sum();
        if (total <= 0) return result;
        var remainders = new double[counts.Count];
        for (var i = 0; i < counts.Count; i++)
        {
            var exact = counts[i] * 100d / total;
            result[i] = (int)Math.Floor(exact);
            remainders[i] = exact - result[i];
        }
        var left = 100 - result.Sum();
        foreach (var i in Enumerable.Range(0, counts.Count).OrderByDescending(i => remainders[i]).ThenBy(i => i).Take(left))
            result[i]++;
        return result;
    }

    /// <summary>Builds the public card model for one poll (null when it does not exist or is hidden).</summary>
    public static async Task<PollCardVM?> BuildCardAsync(AppDbContext db, int pollId, string? voterId)
    {
        var poll = await db.Polls.AsNoTracking().Where(x => x.Id == pollId && x.IsActive)
            .Select(x => new
            {
                x.Id, x.Title, x.Description, x.PollType, x.EndsAt,
                Image = x.FeaturedMedia.Url, // largest size (xl); uncropped for poll uploads
                Options = x.Options.OrderBy(o => o.SortOrder).Select(o => new { o.Id, o.Text }).ToList()
            }).FirstOrDefaultAsync();
        if (poll == null) return null;

        var counts = await db.PollVotes.AsNoTracking().Where(v => v.PollId == pollId)
            .GroupBy(v => v.OptionId).Select(g => new { OptionId = g.Key, Count = g.Count() }).ToListAsync();
        var perOption = poll.Options.Select(o => counts.FirstOrDefault(c => c.OptionId == o.Id)?.Count ?? 0).ToList();
        var percents = Percentages(perOption);

        var voted = false;
        if (!string.IsNullOrEmpty(voterId))
        {
            var hash = VoterHash(pollId, voterId);
            voted = await db.PollVotes.AsNoTracking().AnyAsync(v => v.PollId == pollId && v.VoterHash == hash);
        }

        return new PollCardVM
        {
            Id = poll.Id, Title = poll.Title, Description = poll.Description,
            TypeLabel = TypeLabel(poll.PollType), IsVersus = poll.PollType == PollType.Versus,
            ImageUrl = poll.Image, EndsAtUtc = DateTime.SpecifyKind(poll.EndsAt, DateTimeKind.Utc),
            HasVoted = voted,
            Options = poll.Options.Select((o, i) => new PollCardOptionVM { Id = o.Id, Text = o.Text, Percent = percents[i], Index = i }).ToList()
        };
    }
}
