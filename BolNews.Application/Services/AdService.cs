using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using BolNews.Application.Models;
using Microsoft.Extensions.Options;

namespace BolNews.Application.Services
{
    public class AdService : IAdService
    {
        private readonly AdOptions _options;

        public AdService(IOptions<AdOptions> options)
        {
            _options = options.Value;
        }

        public string GetHeaderAd()
        {
            return _options.Enabled ? _options.HeaderAd : string.Empty;
        }

        public string GetSidebarAd()
        {
            return _options.Enabled ? _options.SidebarAd : string.Empty;
        }

        public string GetInArticleAd()
        {
            return _options.Enabled ? _options.InArticleAd : string.Empty;
        }
    }
}
