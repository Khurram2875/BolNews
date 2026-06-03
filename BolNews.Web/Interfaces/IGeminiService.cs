namespace BolNews.Web.Interfaces
{
    public interface IGeminiService
    {
        Task<(string MetaTitle, string MetaDescription)> GenerateMetadataAsync(string title, string summary, string category);
    }
}
