using BolNews.Web.Models;

namespace BolNews.Web.Interfaces
{
    public interface IHeadlineService
    {
        HeadlineSuggestionResult Generate(string title);
    }
}
