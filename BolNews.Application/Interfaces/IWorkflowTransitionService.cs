using BolNews.Domain.Entities;
using BolNews.Domain.Enums;

namespace BolNews.Application.Interfaces
{
    public interface IWorkflowTransitionService
    {
        Task ExecuteTransitionAsync(
            Article article,
            ArticleWorkflowStatus targetStatus,
            string currentUserId,
            IList<string> roles,
            string? reason = null);
    }
}
