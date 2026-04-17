using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BolNews.Application.Interfaces
{
    public interface IImageService
    {
        Task<string> SaveAuthorImageAsync(Stream stream, int authorId, string rootPath);
        void DeleteAuthorImage(int authorId, string rootPath);


        Task<(string thumb, string medium, string large, string xl)>
        SaveArticleImagesAsync(Stream stream, int articleId, string rootPath);

        void DeleteArticleImages(int articleId, string rootPath);
    }
}
