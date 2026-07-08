using BolNews.Application.DTOs.Grammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IGrammarService
    {
        Task<IReadOnlyList<GrammarIssueDto>> CheckAsync(
            string text,
            string language = "en-US",
            CancellationToken cancellationToken = default);
    }
}
