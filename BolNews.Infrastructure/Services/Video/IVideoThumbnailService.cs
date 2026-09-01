using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services.Video
{
    public interface IVideoThumbnailService
    {
        Task GenerateThumbnailAsync(
            string videoPath,
            string thumbnailPath,
            CancellationToken cancellationToken = default);
    }
}
