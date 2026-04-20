namespace BolNews.Web.Interfaces
{
    public interface IInternalLinkingService
    {
        Task<string> InjectInternalLinksAsync(string content);
    }
}
