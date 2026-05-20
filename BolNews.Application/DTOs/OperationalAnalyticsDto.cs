using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class OperationalAnalyticsDto
    {
        public double AverageReviewHours { get; set; }

        public double AverageFactCheckHours { get; set; }

        public double AverageApprovalToPublishHours { get; set; }

        public int TotalActiveWorkflowItems { get; set; }

        public int TotalOverdueItems { get; set; }

        public double SlaCompliancePercent { get; set; }

        public string BottleneckQueue { get; set; } = string.Empty;
    }
}
