using BolNews.Application.DTOs;

namespace BolNews.Web.Areas.Admin.ViewModels
{
    public class VideoSectionVM
    {
        public List<MediaAssetDto> Videos { get; set; } = new();

        public int Page { get; set; }

        public bool HasNextPage { get; set; }

        public string PageTitle { get; set; } = "Videos";
    }
}
