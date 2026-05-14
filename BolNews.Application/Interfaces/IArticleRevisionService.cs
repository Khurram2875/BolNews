using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface IArticleRevisionService
    {
        Task CreateSnapshotAsync(
            Article article,
            string changedByUserId,
            string workflowState,
            string? changeReason = null);
    }
}
