using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Integration
{
    /// <summary>
    /// Tests for AnalyticsService.
    /// Verifies the atomic upsert pattern, CTR calculation,
    /// and that concurrent-style calls accumulate correctly.
    /// </summary>
    public class AnalyticsServiceTests
    {
        // ── TrackImpressionAsync ──────────────────────────────────────────────

        [Fact]
        public async Task TrackImpression_FirstCall_CreatesRowWithCountOne()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            await service.TrackImpressionAsync(articleId: 1);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 1 && a.Date == DateTime.UtcNow.Date);

            record.Should().NotBeNull("a new row must be created on first impression");
            record!.Impressions.Should().Be(1);
        }

        [Fact]
        public async Task TrackImpression_CalledTwice_AccumulatesCount()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            await service.TrackImpressionAsync(articleId: 1);
            await service.TrackImpressionAsync(articleId: 1);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 1 && a.Date == DateTime.UtcNow.Date);

            record!.Impressions.Should().Be(2, "two impression calls should yield count of 2");
        }

        [Fact]
        public async Task TrackImpression_CalledTenTimes_CountIsTen()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            for (int i = 0; i < 10; i++)
                await service.TrackImpressionAsync(articleId: 1);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 1 && a.Date == DateTime.UtcNow.Date);

            record!.Impressions.Should().Be(10);
        }

        // ── TrackClickAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task TrackClick_FirstCall_CreatesRowWithClickOne()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            await service.TrackClickAsync(articleId: 2);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 2 && a.Date == DateTime.UtcNow.Date);

            record.Should().NotBeNull();
            record!.Clicks.Should().Be(1);
        }

        [Fact]
        public async Task TrackClick_CalledThreeTimes_CountIsThree()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            await service.TrackClickAsync(articleId: 2);
            await service.TrackClickAsync(articleId: 2);
            await service.TrackClickAsync(articleId: 2);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 2 && a.Date == DateTime.UtcNow.Date);

            record!.Clicks.Should().Be(3);
        }

        // ── Impressions and Clicks are independent ────────────────────────────

        [Fact]
        public async Task TrackImpression_DoesNotAffectClicks()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            await service.TrackImpressionAsync(articleId: 1);
            await service.TrackImpressionAsync(articleId: 1);

            var record = context.ArticleAnalytics
                .FirstOrDefault(a => a.ArticleId == 1 && a.Date == DateTime.UtcNow.Date);

            record!.Clicks.Should().Be(0, "tracking impressions must not increment clicks");
        }

        // ── GetCTRAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetCTR_NoImpressions_ReturnsZero()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            var ctr = await service.GetCTRAsync(articleId: 99); // no data

            ctr.Should().Be(0, "CTR with zero impressions must be 0, not a divide-by-zero error");
        }

        [Fact]
        public async Task GetCTR_KnownImpressionAndClicks_ReturnsCorrectPercentage()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            // Seed: 100 impressions, 5 clicks = 5% CTR
            context.ArticleAnalytics.Add(new ArticleAnalytics
            {
                ArticleId   = 1,
                Date        = DateTime.UtcNow.Date.AddDays(-1),
                Impressions = 100,
                Clicks      = 5
            });
            await context.SaveChangesAsync();

            var ctr = await service.GetCTRAsync(articleId: 1);

            ctr.Should().BeApproximately(5.0, precision: 0.001,
                "5 clicks from 100 impressions = 5% CTR");
        }

        [Fact]
        public async Task GetCTR_AggregatesAcrossMultipleDays()
        {
            var context = TestDbContextFactory.CreateWithSeed();
            var service = new AnalyticsService(context);

            // Day 1: 200 impressions, 10 clicks
            // Day 2: 300 impressions, 20 clicks
            // Total: 500 impressions, 30 clicks = 6% CTR
            context.ArticleAnalytics.AddRange(
                new ArticleAnalytics { ArticleId = 1, Date = DateTime.UtcNow.Date.AddDays(-2), Impressions = 200, Clicks = 10 },
                new ArticleAnalytics { ArticleId = 1, Date = DateTime.UtcNow.Date.AddDays(-1), Impressions = 300, Clicks = 20 }
            );
            await context.SaveChangesAsync();

            var ctr = await service.GetCTRAsync(articleId: 1);

            ctr.Should().BeApproximately(6.0, precision: 0.001,
                "CTR must be computed across all days, not just one");
        }
    }
}
