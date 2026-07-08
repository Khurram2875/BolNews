using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs.Grammar
{
    public sealed class GrammarIssueDto
    {
        public int Id { get; set; }

        public int Offset { get; set; }

        public int Length { get; set; }

        public string Original { get; set; } = string.Empty;

        public string Replacement { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string RuleId { get; set; } = string.Empty;
    }
}
