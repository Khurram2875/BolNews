using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface IGoldRateService
    {
        Task<GoldDto> GetGoldRateAsync();
    }
}
