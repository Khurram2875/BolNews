using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Domain.Entities;

namespace BolNews.Application.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly INotificationRealtimeService _realtimeService;

        public NotificationService(INotificationRepository repo, INotificationRealtimeService realtimeService)
        {
            _repo = repo;
            _realtimeService = realtimeService;
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

            await _repo.AddAsync(notification);

            await _realtimeService.PushAsync(userId);
        }

        public Task<List<Notification>> GetUnreadAsync(string userId)
            => _repo.GetUnreadAsync(userId);

        public Task MarkReadAsync(int notificationId)
            => _repo.MarkReadAsync(notificationId);
    }
}
