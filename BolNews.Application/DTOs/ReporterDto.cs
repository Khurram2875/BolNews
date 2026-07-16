namespace BolNews.Application.DTOs
{
    public class ReporterDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? SourceName { get; set; }

        public string? Slug { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(SourceName)
                ? Name
                : $"{Name} ({SourceName})";
    }
}
