using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly INotificationRealtimeService _realtimeService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(INotificationRepository repo, INotificationRealtimeService realtimeService, ILogger<NotificationService> logger)
        {
            _repo = repo;
            _realtimeService = realtimeService;
            _logger = logger;
        }

        public async Task NotifyAsync(string userId,string title,string message,string? url = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = url,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogDebug("Notification sent to {UserId}: {Title}",userId,title);

            await _repo.AddAsync(notification);

            await _realtimeService.PushAsync(userId);
        }

        public Task<List<Notification>> GetUnreadAsync(string userId)
            => _repo.GetUnreadAsync(userId);

        public Task<bool> MarkReadAsync(int notificationId, string userId)
            => _repo.MarkReadAsync(notificationId, userId);
    }
}
