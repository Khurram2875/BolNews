using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Application.Interfaces
{
    public interface IAdService
    {
        string GetHeaderAd();
        string GetSidebarAd();
        string GetInArticleAd();
    }
}
