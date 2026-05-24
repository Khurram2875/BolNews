using BolNews.Application.Interfaces;
using System.Collections.Concurrent;

namespace BolNews.Web.Services
{
    public class PresenceTracker : IPresenceTracker
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>>
            DiscussionPresence = new();

        public void JoinDiscussion(string articleId, string userId)
        {
            if (!DiscussionPresence.ContainsKey(articleId))
            {
                DiscussionPresence[articleId] = new HashSet<string>();
            }

            lock (DiscussionPresence[articleId])
            {
                DiscussionPresence[articleId].Add(userId);
            }
        }

        public void LeaveDiscussion(string articleId, string userId)
        {
            if (!DiscussionPresence.ContainsKey(articleId))
                return;

            lock (DiscussionPresence[articleId])
            {
                DiscussionPresence[articleId].Remove(userId);
            }
        }

        public bool IsUserInDiscussion(string articleId, string userId)
        {
            if (!DiscussionPresence.ContainsKey(articleId))
                return false;

            lock (DiscussionPresence[articleId])
            {
                return DiscussionPresence[articleId].Contains(userId);
            }
        }

        public int GetDiscussionCount(string articleId)
        {
            if (!DiscussionPresence.ContainsKey(articleId))
                return 0;

            lock (DiscussionPresence[articleId])
            {
                return DiscussionPresence[articleId].Count;
            }
        }
    }
}