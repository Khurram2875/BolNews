using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IArticleLockService
    {
        Task<bool> TryAcquireLockAsync(Article article, string currentUserId);
        bool IsLockedByAnotherUser(Article article, string currentUserId);
        Task<string?> GetLockOwnerNameAsync(Article article, string currentUserId);
        Task ReleaseLockAsync(Article article, string currentUserId);
        Task RefreshLockAsync(Article article, string currentUserId);
    }
}
