using BolNews.Application.Services;
using BolNews.Domain.Entities;
using BolNews.Persistence.Repositories;
using BolNews.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace BolNews.Tests.Integration
{
    /// <summary>
    /// Uses SQLite in-memory (not EF InMemory) because ExecuteUpdateAsync
    /// requires a relational provider.
    /// </summary>
    public class AnalyticsServiceTests
    {
        // ── TrackImpressionAsync ──────────────────────────────────────────────

        [Fact]
        public async Task TrackImpression_FirstCall_CreatesRowWithCountOne()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                await service.TrackImpressionAsync(articleId: 1);

                var records = await repo.GetByArticleIdAsync(1);
                var today = records.FirstOrDefault(a => a.Date == DateTime.UtcNow.Date);

                today.Should().NotBeNull("a new row must be created on first impression");
                today!.Impressions.Should().Be(1);
            }
        }

        [Fact]
        public async Task TrackImpression_CalledTwice_AccumulatesCount()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                await service.TrackImpressionAsync(articleId: 1);
                await service.TrackImpressionAsync(articleId: 1);

                var records = await repo.GetByArticleIdAsync(1);
                records.Sum(r => r.Impressions).Should().Be(2);
            }
        }

        [Fact]
        public async Task TrackImpression_CalledTenTimes_CountIsTen()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                for (int i = 0; i < 10; i++)
                    await service.TrackImpressionAsync(articleId: 1);

                var records = await repo.GetByArticleIdAsync(1);
                records.Sum(r => r.Impressions).Should().Be(10);
            }
        }

        // ── TrackClickAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task TrackClick_FirstCall_CreatesRowWithClickOne()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                await service.TrackClickAsync(articleId: 2);

                var records = await repo.GetByArticleIdAsync(2);
                var today = records.FirstOrDefault(a => a.Date == DateTime.UtcNow.Date);

                today.Should().NotBeNull();
                today!.Clicks.Should().Be(1);
            }
        }

        [Fact]
        public async Task TrackClick_CalledThreeTimes_CountIsThree()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                await service.TrackClickAsync(articleId: 2);
                await service.TrackClickAsync(articleId: 2);
                await service.TrackClickAsync(articleId: 2);

                var records = await repo.GetByArticleIdAsync(2);
                records.Sum(r => r.Clicks).Should().Be(3);
            }
        }

        // ── Impressions and Clicks are independent ────────────────────────────

        [Fact]
        public async Task TrackImpression_DoesNotAffectClicks()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var repo = new AnalyticsRepository(context);
                var service = new AnalyticsService(repo);

                await service.TrackImpressionAsync(articleId: 1);
                await service.TrackImpressionAsync(articleId: 1);

                var records = await repo.GetByArticleIdAsync(1);
                records.Sum(r => r.Clicks).Should().Be(0,
                    "tracking impressions must not increment clicks");
            }
        }

        // ── GetCTRAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetCTR_NoImpressions_ReturnsZero()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                var service = new AnalyticsService(new AnalyticsRepository(context));
                var ctr = await service.GetCTRAsync(articleId: 99);
                ctr.Should().Be(0, "CTR with zero impressions must be 0");
            }
        }

        [Fact]
        public async Task GetCTR_KnownImpressionAndClicks_ReturnsCorrectPercentage()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                context.ArticleAnalytics.Add(new ArticleAnalytics
                {
                    ArticleId = 1,
                    Date = DateTime.UtcNow.Date.AddDays(-1),
                    Impressions = 100,
                    Clicks = 5
                });
                await context.SaveChangesAsync();

                var service = new AnalyticsService(new AnalyticsRepository(context));
                var ctr = await service.GetCTRAsync(articleId: 1);

                ctr.Should().BeApproximately(5.0, precision: 0.001,
                    "5 clicks from 100 impressions = 5% CTR");
            }
        }

        [Fact]
        public async Task GetCTR_AggregatesAcrossMultipleDays()
        {
            var (context, conn) = TestDbContextFactory.CreateSqliteWithSeed();
            await using (conn) await using (context)
            {
                context.ArticleAnalytics.AddRange(
                    new ArticleAnalytics { ArticleId = 1, Date = DateTime.UtcNow.Date.AddDays(-2), Impressions = 200, Clicks = 10 },
                    new ArticleAnalytics { ArticleId = 1, Date = DateTime.UtcNow.Date.AddDays(-1), Impressions = 300, Clicks = 20 }
                );
                await context.SaveChangesAsync();

                var service = new AnalyticsService(new AnalyticsRepository(context));
                var ctr = await service.GetCTRAsync(articleId: 1);

                ctr.Should().BeApproximately(6.0, precision: 0.001,
                    "CTR must aggregate across all days");
            }
        }
    }
}