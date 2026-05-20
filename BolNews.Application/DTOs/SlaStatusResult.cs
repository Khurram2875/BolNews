using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs
{
    public class SlaStatusResult
    {
        public bool IsTracked { get; set; }

        public bool IsOverdue { get; set; }

        public TimeSpan Elapsed { get; set; }

        public TimeSpan Threshold { get; set; }

        public string StatusLabel { get; set; } = string.Empty;
    }
}
