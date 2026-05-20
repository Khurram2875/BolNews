using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services
{
    public class OperationalAnalyticsService : IOperationalAnalyticsService
    {
        private readonly IArticleRepository _articleRepository;
        private readonly ISlaService _slaService;

        public OperationalAnalyticsService(
            IArticleRepository articleRepository,
            ISlaService slaService)
        {
            _articleRepository = articleRepository;
            _slaService = slaService;
        }

        public async Task<OperationalAnalyticsDto> GetAnalyticsAsync()
        {
            var articles =
                await _articleRepository.GetActiveWorkflowArticlesAsync();

            var active = articles.Count;

            var overdue =
                articles.Count(a =>
                {
                    var sla = _slaService.Evaluate(a);
                    return sla.IsTracked && sla.IsOverdue;
                });

            var submitted =
                articles.Count(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.Submitted);

            var underReview =
                articles.Count(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.UnderReview);

            var factCheck =
                articles.Count(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.FactCheckPending);

            var approved =
                articles.Count(a =>
                    a.WorkflowStatus == ArticleWorkflowStatus.Approved);

            var bottleneck = new Dictionary<string, int>
        {
            { "Submitted", submitted },
            { "Under Review", underReview },
            { "Fact Check", factCheck },
            { "Approved", approved }
        }
            .OrderByDescending(x => x.Value)
            .FirstOrDefault()
            .Key ?? "None";

            var reviewDurations =
                articles
                    .Where(a =>
                        a.SubmittedAt.HasValue &&
                        a.ReviewStartedAt.HasValue)
                    .Select(a =>
                        (a.ReviewStartedAt.Value - a.SubmittedAt.Value)
                            .TotalHours)
                    .ToList();

            var factCheckDurations =
                articles
                    .Where(a =>
                        a.ReviewStartedAt.HasValue &&
                        a.FactCheckStartedAt.HasValue)
                    .Select(a =>
                        (a.FactCheckStartedAt.Value - a.ReviewStartedAt.Value)
                            .TotalHours)
                    .ToList();

            var publishDurations =
                articles
                    .Where(a =>
                        a.ApprovedAt.HasValue &&
                        a.PublishedAt.HasValue)
                    .Select(a =>
                        (a.PublishedAt.Value - a.ApprovedAt.Value)
                            .TotalHours)
                    .ToList();

            return new OperationalAnalyticsDto
            {
                TotalActiveWorkflowItems = active,
                TotalOverdueItems = overdue,

                SlaCompliancePercent =
                    active == 0
                        ? 100
                        : ((double)(active - overdue) / active) * 100,

                AverageReviewHours =
                    reviewDurations.Any()
                        ? reviewDurations.Average()
                        : 0,

                AverageFactCheckHours =
                    factCheckDurations.Any()
                        ? factCheckDurations.Average()
                        : 0,

                AverageApprovalToPublishHours =
                    publishDurations.Any()
                        ? publishDurations.Average()
                        : 0,

                BottleneckQueue = bottleneck
            };
        }
    }
}
