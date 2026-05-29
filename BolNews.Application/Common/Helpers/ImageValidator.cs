using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BolNews.Application.Common.Helpers
{
    public static class ImageValidator
    {
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp" };

        private const int MaxFileSize = 2 * 1024 * 1024; // 2MB

        public static bool IsValid(IFormFile file, out string error)
        {
            error = string.Empty;

            if (file == null || file.Length == 0)
            {
                error = "No file uploaded.";
                return false;
            }

            var allowedMimeTypes =
                new[] { "image/jpeg", "image/png", "image/webp" };

            if (!allowedMimeTypes.Contains(file.ContentType))
            {
                error = "Invalid file type.";
                return false;
            }

            if (file.Length > MaxFileSize)
            {
                error = "File size must be less than 2MB.";
                return false;
            }

            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                error = "Only JPG, PNG and WEBP formats are allowed.";
                return false;
            }

            return true;
        }
    }
}
