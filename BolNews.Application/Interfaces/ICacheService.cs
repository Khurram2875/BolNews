using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;

namespace BolNews.Application.Interfaces
{
    public interface ICacheService
    {
        Task<T> GetOrCreateAsync<T>(
             string key,
             Func<Task<T>> factory,
             int minutes = 10
         );

        void Remove(string key);
    }
}
