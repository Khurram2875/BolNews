using BolNews.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;




namespace BolNews.Infrastructure.Services
{
    public class ImageService : IImageService
    {
        public async Task<string> SaveAuthorImageAsync(Stream stream, int authorId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "authors", authorId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, "profile.webp");

            using (var image = await Image.LoadAsync(stream))
            {
                await image.SaveAsync(filePath, new WebpEncoder
                {
                    Quality = 75
                });
            }

            return $"/uploads/authors/{authorId}/profile.webp";
        }

        public void DeleteAuthorImage(int authorId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "authors", authorId.ToString()+".webp");

            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }
    }
}
