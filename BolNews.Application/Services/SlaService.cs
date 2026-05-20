using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Services
{
    public class SlaService : ISlaService
    {
        public SlaStatusResult Evaluate(Article article)
        {
            var now = DateTime.UtcNow;

            switch (article.WorkflowStatus)
            {
                case ArticleWorkflowStatus.Submitted:
                    return BuildResult(
                        article.SubmittedAt,
                        TimeSpan.FromHours(2),
                        "Submitted",
                        now);

                case ArticleWorkflowStatus.UnderReview:
                    return BuildResult(
                        article.ReviewStartedAt,
                        TimeSpan.FromHours(4),
                        "Under Review",
                        now);

                case ArticleWorkflowStatus.FactCheckPending:
                    return BuildResult(
                        article.FactCheckStartedAt,
                        TimeSpan.FromHours(2),
                        "Fact Check",
                        now);

                case ArticleWorkflowStatus.Approved:
                    return BuildResult(
                        article.ApprovedAt,
                        TimeSpan.FromHours(1),
                        "Approved",
                        now);

                default:
                    return new SlaStatusResult
                    {
                        IsTracked = false
                    };
            }
        }

        private static SlaStatusResult BuildResult(
            DateTime? startedAt,
            TimeSpan threshold,
            string label,
            DateTime now)
        {
            if (!startedAt.HasValue)
            {
                return new SlaStatusResult
                {
                    IsTracked = false
                };
            }

            var elapsed = now - startedAt.Value;

            return new SlaStatusResult
            {
                IsTracked = true,
                Threshold = threshold,
                Elapsed = elapsed,
                IsOverdue = elapsed > threshold,
                StatusLabel = label
            };
        }
    }
}
