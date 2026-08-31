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

            using var image = await Image.LoadAsync(stream);
            await image.SaveAsync(filePath, new WebpEncoder { Quality = 75 });

            return $"/uploads/authors/{authorId}/profile.webp";
        }

        // Fix #6: was appending ".webp" to the folder name — now correctly targets
        // the author's directory and deletes it recursively.
        public void DeleteAuthorImage(int authorId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "authors", authorId.ToString());

            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }

        public async Task<(string thumb, string medium, string large, string xl)>
            SaveArticleImagesAsync(Stream stream, int articleId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "articles", articleId.ToString());

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            stream.Position = 0;
            using var image = await Image.LoadAsync(stream);

            // THUMB 150×150
            var thumbPath = Path.Combine(folderPath, "thumb.webp");
            using (var thumb = image.Clone(x => x.Resize(new ResizeOptions
            { Size = new Size(150, 150), Mode = ResizeMode.Crop })))
                await thumb.SaveAsync(thumbPath, new WebpEncoder { Quality = 75 });

            // MEDIUM 400×250
            var mediumPath = Path.Combine(folderPath, "medium.webp");
            using (var medium = image.Clone(x => x.Resize(new ResizeOptions
            { Size = new Size(400, 250), Mode = ResizeMode.Crop })))
                await medium.SaveAsync(mediumPath, new WebpEncoder { Quality = 80 });

            // LARGE 800×450
            var largePath = Path.Combine(folderPath, "large.webp");
            using (var large = image.Clone(x => x.Resize(new ResizeOptions
            { Size = new Size(800, 450), Mode = ResizeMode.Crop })))
                await large.SaveAsync(largePath, new WebpEncoder { Quality = 85 });

            // XL 1200×675
            var xlPath = Path.Combine(folderPath, "xl.webp");
            using (var xl = image.Clone(x => x.Resize(new ResizeOptions
            { Size = new Size(1200, 675), Mode = ResizeMode.Crop })))
                await xl.SaveAsync(xlPath, new WebpEncoder { Quality = 90 });

            return (
                $"/uploads/articles/{articleId}/thumb.webp",
                $"/uploads/articles/{articleId}/medium.webp",
                $"/uploads/articles/{articleId}/large.webp",
                $"/uploads/articles/{articleId}/xl.webp"
            );
        }

        public void DeleteArticleImages(int articleId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "articles", articleId.ToString());

            if (Directory.Exists(folderPath))
                Directory.Delete(folderPath, true);
        }

        public async Task<(string thumb, string medium, string large, string xl)> SaveMediaImagesAsync(Stream stream, int mediaId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "media", mediaId.ToString());
            Directory.CreateDirectory(folderPath);
            stream.Position = 0;
            using var image = await Image.LoadAsync(stream);
            async Task Save(string name, Size size, ResizeMode mode, int quality)
            {
                using var copy = image.Clone(x => x.Resize(new ResizeOptions { Size = size, Mode = mode }));
                await copy.SaveAsync(Path.Combine(folderPath, name), new WebpEncoder { Quality = quality });
            }
            await Save("thumb.webp", new Size(150, 150), ResizeMode.Crop, 75);
            await Save("medium.webp", new Size(400, 250), ResizeMode.Crop, 80);
            await Save("large.webp", new Size(800, 450), ResizeMode.Crop, 85);
            await Save("xl.webp", new Size(1200, 675), ResizeMode.Crop, 90);
            return ($"/uploads/media/{mediaId}/thumb.webp", $"/uploads/media/{mediaId}/medium.webp", $"/uploads/media/{mediaId}/large.webp", $"/uploads/media/{mediaId}/xl.webp");
        }

        public async Task<string> SaveMediaFileAsync(Stream stream, string fileName, int mediaId, string rootPath)
        {
            var folderPath = Path.Combine(rootPath, "uploads", "media", mediaId.ToString());
            Directory.CreateDirectory(folderPath);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var safeName = $"original{extension}";
            await using var output = File.Create(Path.Combine(folderPath, safeName));
            await stream.CopyToAsync(output);
            return $"/uploads/media/{mediaId}/{safeName}";
        }
        public async Task<string> SaveArticleContentImageAsync(Stream stream, string fileName, string rootPath)
        {
            var folderPath = Path.Combine(
                rootPath,
                "uploads",
                "articles",
                "content");

            Directory.CreateDirectory(folderPath);

            var extensionlessName =
                Path.GetFileNameWithoutExtension(fileName);

            var safeName =
                $"{extensionlessName}-{Guid.NewGuid():N}.webp";

            var filePath =
                Path.Combine(folderPath, safeName);

            stream.Position = 0;

            using var image =
                await Image.LoadAsync(stream);

            await image.SaveAsync(
                filePath,
                new WebpEncoder
                {
                    Quality = 85
                });

            return $"/uploads/articles/content/{safeName}";
        }
    }
}
