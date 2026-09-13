namespace BolNews.Application.DTOs
{
    public class TagDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;
    }

    public class PublishedTagSitemapDto
    {
        public string Slug { get; set; } = string.Empty;

        public DateTime LastModified { get; set; }
    }
}
