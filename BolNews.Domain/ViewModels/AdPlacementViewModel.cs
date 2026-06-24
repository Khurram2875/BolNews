namespace BolNews.Domains.ViewModels
{
    public class AdPlacementViewModel
    {
        public string PlacementKey { get; set; } = string.Empty;

        public string? AdCode { get; set; }

        public bool IsEnabled { get; set; }
    }
}
