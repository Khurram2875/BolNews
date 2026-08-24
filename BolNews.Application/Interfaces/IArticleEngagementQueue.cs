using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IArticleEngagementQueue
    {
        bool TryEnqueue(int articleId, bool incrementViewCount);
    }
}
