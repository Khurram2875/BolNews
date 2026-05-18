using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(
            string userId,
            string title,
            string message,
            string? url = null);

        Task<List<Notification>> GetUnreadAsync(string userId);

        Task MarkReadAsync(int notificationId);
    }
}
