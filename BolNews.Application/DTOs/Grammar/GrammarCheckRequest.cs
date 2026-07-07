using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.DTOs.Grammar
{
    public sealed class GrammarCheckRequest
    {
        public string Text { get; set; } = string.Empty;

        public string Language { get; set; } = "en-US";
    }
}
