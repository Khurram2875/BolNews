using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IPresenceTracker
    {
        void JoinDiscussion(string articleId, string userId);

        void LeaveDiscussion(string articleId, string userId);

        bool IsUserInDiscussion(string articleId, string userId);

        int GetDiscussionCount(string articleId);
    }
}
