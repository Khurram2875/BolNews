using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IEditorialAssignmentService
    {
        Task AssignReviewerAsync(
            Article article,
            string reviewerUserId,
            string currentUserId,
            IList<string> roles);

        Task AssignFactCheckerAsync(
            Article article,
            string factCheckerUserId,
            string currentUserId,
            IList<string> roles);
    }
}
