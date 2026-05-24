using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BolNews.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, HashSet<string>>
        DiscussionPresence = new();
        private readonly IPresenceTracker _presenceTracker;

        public NotificationHub(IPresenceTracker presenceTracker)
        {
            _presenceTracker = presenceTracker;
        }
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    $"user-{userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.RemoveFromGroupAsync(
                    Context.ConnectionId,
                    $"user-{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
        public async Task JoinDiscussion(string articleId)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                $"discussion-{articleId}");

            _presenceTracker.JoinDiscussion(
                articleId,
                Context.UserIdentifier!);

            await Clients.Group($"discussion-{articleId}")
                .SendAsync(
                    "DiscussionPresenceUpdated",
                    _presenceTracker.GetDiscussionCount(articleId));
        }

        public async Task LeaveDiscussion(string articleId)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                $"discussion-{articleId}");

            _presenceTracker.LeaveDiscussion(
                articleId,
                Context.UserIdentifier!);

            await Clients.Group($"discussion-{articleId}")
                .SendAsync(
                    "DiscussionPresenceUpdated",
                    _presenceTracker.GetDiscussionCount(articleId));
        }

        public async Task TypingStarted(string articleId)
        {
            var userName = Context.User?.Identity?.Name ?? "Newsroom User";

            await Clients.OthersInGroup($"discussion-{articleId}")
                .SendAsync(
                    "DiscussionTypingStarted",
                    userName);
        }

        public async Task TypingStopped(string articleId)
        {
            await Clients.OthersInGroup($"discussion-{articleId}")
                .SendAsync("DiscussionTypingStopped");
        }
    }
}
