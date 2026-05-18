using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Domain.Enums
{
    public enum ArticleWorkflowStatus
    {
        Draft = 0,
        Submitted = 1,
        UnderReview = 2,
        FactCheckPending = 3,
        Approved = 4,
        Rejected = 5,
        Published = 6,
        Archived = 7
    }
}
