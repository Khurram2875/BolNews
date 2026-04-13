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
        public async Task<(string thumb, string medium, string large)>
    SaveArticleImagesAsync(Stream stream, int articleId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "articles", articleId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            stream.Position = 0;
            using var image = await Image.LoadAsync(stream);

            // 🔹 THUMB (150x150)
            var thumbPath = Path.Combine(folderPath, "thumb.webp");
            var thumbImage = image.Clone(x => x.Resize(150, 150));
            await thumbImage.SaveAsync(thumbPath, new WebpEncoder { Quality = 75 });

            // 🔹 MEDIUM (400x250)
            var mediumPath = Path.Combine(folderPath, "medium.webp");
            var mediumImage = image.Clone(x => x.Resize(400, 250));
            await mediumImage.SaveAsync(mediumPath, new WebpEncoder { Quality = 80 });

            // 🔹 LARGE (800x450)
            var largePath = Path.Combine(folderPath, "large.webp");
            var largeImage = image.Clone(x => x.Resize(800, 450));
            await largeImage.SaveAsync(largePath, new WebpEncoder { Quality = 85 });

            return (
                $"/uploads/articles/{articleId}/thumb.webp",
                $"/uploads/articles/{articleId}/medium.webp",
                $"/uploads/articles/{articleId}/large.webp"
            );
        }
        public void DeleteArticleImages(int articleId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "articles", articleId.ToString());

            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }
    }

}
