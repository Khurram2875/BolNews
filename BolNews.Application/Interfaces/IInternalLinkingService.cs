namespace BolNews.Application.Interfaces
{
    public interface IInternalLinkingService
    {
        Task<string> InjectInternalLinksAsync(string content);
    }
}
