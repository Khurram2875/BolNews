using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BolNews.Web.Services
{
    public class NotificationRealtimeService : INotificationRealtimeService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationRealtimeService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task PushAsync(string userId)
        {
            await _hubContext.Clients
                .Group($"user-{userId}")
                .SendAsync("NotificationReceived");
        }
    }
}
