using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BolNews.Application.DTOs;
using BolNews.Domain.Entities;

namespace BolNews.Application.Interfaces
{
    public interface ISlaService
    {
        SlaStatusResult Evaluate(Article article);
    }
}
