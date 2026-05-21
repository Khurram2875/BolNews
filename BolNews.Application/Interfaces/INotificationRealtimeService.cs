using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface INotificationRealtimeService
    {
        Task PushAsync(string userId);
        Task SendDiscussionUpdateAsync(IEnumerable<string> userIds, object payload);
    }
}
