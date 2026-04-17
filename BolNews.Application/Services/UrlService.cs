using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BolNews.Application.Services
{
    public class UrlService : IUrlService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UrlService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }
    }
}
